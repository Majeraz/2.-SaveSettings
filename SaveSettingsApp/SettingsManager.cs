using JSONFilesManagerProj;
using System.Reflection;
using System.IO;


namespace SaveSettingsProject;

/// <summary>
/// [Utrata ustawien 2026-08-20] Jak zakonczylo sie wczytanie pliku ustawien. Wolajacy (aplikacja) moze to
/// zaraportowac do telemetrii - <see cref="DefaultsAfterLoss"/> to AWARIA (user traci konfiguracje), a nie
/// zwykly stan poczatkowy, i bez tego rozroznienia wyglada z zewnatrz identycznie jak pierwsze uruchomienie.
/// </summary>
public enum SettingsLoadOutcome {
    /// <summary>Plik wczytany normalnie.</summary>
    LoadedFromFile,
    /// <summary>Glowny plik byl pusty/uszkodzony, ale udalo sie odtworzyc z kopii zapasowej (.bak).</summary>
    RecoveredFromBackup,
    /// <summary>Pliku nie bylo - pierwsze uruchomienie na tym stanowisku. Stan normalny.</summary>
    FreshDefaults,
    /// <summary>Plik BYL, ale nie dalo sie go odczytac ani odtworzyc z kopii - ustawienia utracone.</summary>
    DefaultsAfterLoss
}

/// <summary>
/// Get acces to JSON File at specified related file path (if it doesn't exist yet it is becoming created).
/// Gives methods to manipulate settings files.
/// </summary>
/// <typeparam name="ObjectType">Type of stored object in JSON file</typeparam>
public class SettingsManager<ObjectType> {
    public string JSONFullFilePath;
    private ObjectType referenceToTheOriginalObject;

    /// <summary>[Utrata ustawien 2026-08-20] Jak poszlo wczytanie tego pliku przy starcie.</summary>
    public SettingsLoadOutcome LoadOutcome { get; private set; } = SettingsLoadOutcome.LoadedFromFile;

    /// <summary>
    /// [Utrata ustawien 2026-08-20] Czytelny powod, gdy <see cref="LoadOutcome"/> to
    /// <see cref="SettingsLoadOutcome.RecoveredFromBackup"/> lub <see cref="SettingsLoadOutcome.DefaultsAfterLoss"/>
    /// (np. "plik pusty (0 znakow)"). Null gdy wczytanie przebieglo normalnie.
    /// </summary>
    public string? LoadFailureReason { get; private set; }

    public SettingsManager(string JSONFileRelativePath, ref ObjectType? originalObject) {
        string appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DXFManager");
        Directory.CreateDirectory(appDataRoot);

        JSONFullFilePath = Path.Combine(appDataRoot, JSONFileRelativePath);

        string previousBasePath = Path.GetDirectoryName(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName)!;
        string previousFullPath = Path.Combine(previousBasePath, JSONFileRelativePath);

        if (!File.Exists(JSONFullFilePath) && File.Exists(previousFullPath)) {
            Directory.CreateDirectory(Path.GetDirectoryName(JSONFullFilePath)!);
            File.Copy(previousFullPath, JSONFullFilePath, overwrite: true);
        }

        originalObject = LoadWithRecovery();
        referenceToTheOriginalObject = originalObject!;
    }

    /// <summary>
    /// [Utrata ustawien 2026-08-20, zgloszenie klienta] Wczytuje ustawienia w trzech podejsciach, od
    /// najlepszego do najgorszego, zamiast dawnego "pusty plik = bierz wartosci domyslne":
    ///
    /// 1. GLOWNY plik - normalna sciezka.
    /// 2. KOPIA ZAPASOWA (.bak, zostawiana przez kazdy udany zapis) - ratuje sytuacje, w ktorej glowny plik
    ///    zostal przerwany w polowie zapisu (tak klient stracil wybor "za kg" i "parametry detalu i procesu").
    ///    Po udanym odczycie kopii OD RAZU odtwarzamy z niej glowny plik, zeby stan na dysku byl spojny.
    /// 3. WARTOSCI DOMYSLNE - dopiero gdy obie proby zawioda. Uszkodzona tresc jest wtedy odkladana na bok
    ///    jako <c>.corrupt-RRRRMMDDGGMMSS</c> (nie kasujemy dowodu - da sie z niego odzyskac ustawienia recznie),
    ///    a wynik jest oznaczany jako <see cref="SettingsLoadOutcome.DefaultsAfterLoss"/> = awaria do telemetrii.
    /// </summary>
    private ObjectType LoadWithRecovery() {
        bool mainFileExisted = File.Exists(JSONFullFilePath);

        if (JSONFilesManager.TryDeserializeJSON<ObjectType>(JSONFullFilePath, out var fromMainFile, out string? mainFailure)) {
            LoadOutcome = SettingsLoadOutcome.LoadedFromFile;
            return fromMainFile!;
        }

        string backupPath = JSONFilesManager.GetBackupPath(JSONFullFilePath);
        if (JSONFilesManager.TryDeserializeJSON<ObjectType>(backupPath, out var fromBackup, out _)) {
            LoadOutcome = SettingsLoadOutcome.RecoveredFromBackup;
            LoadFailureReason = mainFailure;
            TryRestoreMainFileFromBackup(backupPath);
            return fromBackup!;
        }

        // Pusty plik (0 bajtow) nie niesie zadnej informacji - odkladamy na bok tylko tresc, ktora cos zawiera.
        TryPreserveCorruptedFile();

        // Plik BYL, a i tak konczymy na wartosciach domyslnych = user stracil konfiguracje (awaria).
        // Pliku NIE BYLO = pierwsze uruchomienie na tym stanowisku (stan normalny).
        LoadOutcome = mainFileExisted ? SettingsLoadOutcome.DefaultsAfterLoss : SettingsLoadOutcome.FreshDefaults;
        LoadFailureReason = mainFailure;

        // [Falszywy alarm 2026-08-24, wdrozenie tenant 77] Dawne CreateJSONFileAndItsDirectory zostawialo tu
        // PUSTY plik (0 bajtow). Przy NASTEPNYM starcie plik "istnial, ale byl pusty" -> DefaultsAfterLoss,
        // czyli telemetria krzyczala "user stracil konfiguracje" przy KAZDEJ swiezej instalacji (raz), a pusty
        // plik na dysku byl stanem posrednim, ktory kazdy odczyt traktuje jak awarie. Teraz od razu zapisujemy
        // WARTOSCI DOMYSLNE zapisem atomowym - na dysku nigdy nie ma pustego pliku, a drugi start czyta normalnie.
        // Best-effort: gdy zapis sie nie uda (dysk/uprawnienia), dziala sie jak dotad - defaulty w pamieci,
        // a plik utworzy pierwszy udany zapis ustawien.
        ObjectType defaults = Activator.CreateInstance<ObjectType>();
        try {
            JSONFilesManager.WriteObjectToJSONFile(JSONFullFilePath, defaults!);
        } catch {
            try { Directory.CreateDirectory(Path.GetDirectoryName(JSONFullFilePath)!); } catch { }
        }
        return defaults;
    }

    private static bool IsEmptyFile(string path) {
        try { return new FileInfo(path).Length == 0; } catch { return false; }
    }

    /// <summary>Odtwarza glowny plik z kopii zapasowej. Best-effort: gdy sie nie uda, w pamieci i tak mamy
    /// juz poprawne ustawienia, a pierwszy zapis (atomowy) naprawi plik.</summary>
    private void TryRestoreMainFileFromBackup(string backupPath) {
        try { File.Copy(backupPath, JSONFullFilePath, overwrite: true); } catch { }
    }

    /// <summary>Odklada nieczytelna tresc jako <c>.corrupt-RRRRMMDDGGMMSS</c> - zeby dalo sie odzyskac
    /// ustawienia recznie i zeby bylo widac, ze cos sie stalo.</summary>
    private void TryPreserveCorruptedFile() {
        try {
            if (!File.Exists(JSONFullFilePath) || IsEmptyFile(JSONFullFilePath))
                return;
            File.Copy(JSONFullFilePath, JSONFullFilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), overwrite: true);
        } catch {
            // Zachowanie dowodu jest dodatkiem, nie moze wywrocic startu aplikacji.
        }
    }

    public void AddObjectToJSONFile(object objectToBeWritten) {
        if (objectToBeWritten.GetType() != typeof(ObjectType))   // if type of file that is supposed to be added to JSON doesn't match type that is placed in JSON
            throw new Exception("Type of the object doesn't match JSON type");
        JSONFilesManager.WriteObjectToJSONFile(JSONFullFilePath, objectToBeWritten);
    }
    /// <summary>
    /// Udates JSON file for the object associated with this SettingsManager object. It can be used for initialize new JSON file.
    ///
    /// [Utrata ustawien 2026-08-20] Dawne <c>File.WriteAllText(path, "")</c> (kasowanie pliku przed zapisem)
    /// USUNIETE - <see cref="JSONFilesManager.WriteObjectToJSONFile"/> podmienia plik atomowo i zostawia kopie
    /// zapasowa. Kasowanie z gory bylo dokladnie tym oknem, w ktorym ubity proces zostawial pusty config.
    /// </summary>
    /// <param name="objectToBeWritten"></param>
    public void RewriteSetting() {
        AddObjectToJSONFile(referenceToTheOriginalObject!);
    }
}
