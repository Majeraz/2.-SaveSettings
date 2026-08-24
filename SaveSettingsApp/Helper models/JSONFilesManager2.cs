using Newtonsoft.Json;

namespace JSONFilesManagerProj;
/// <summary>
/// Creates JSON file and operate on it
/// </summary>
public static class JSONFilesManager {

    /// <summary>
    /// [Przenosiny typow 2026-08-24] JEDNO wspolne zrodlo ustawien serializera (wczesniej 3 kopie inline).
    /// TypeNameHandling.All jak dotad + <see cref="AssemblyAgnosticSerializationBinder"/>: odczyt znajduje
    /// typ po pelnej nazwie takze wtedy, gdy klasa przeniosla sie do innej biblioteki (ekstrakcja E2:
    /// "UserModel, DXF Manager" -> assembly DXFManagerBackend). Bez bindera taki plik = "uszkodzony"
    /// i reset ustawien u kazdego klienta po aktualizacji. Zapis dostaje AKTUALNE nazwy assembly.
    /// </summary>
    private static JsonSerializerSettings CreateSerializerSettings() => new() {
        TypeNameHandling = TypeNameHandling.All,
        SerializationBinder = new AssemblyAgnosticSerializationBinder(),
    };

    /// <summary>
    /// Serialize object and write it to JSON file at specified path.
    ///
    /// [Utrata ustawien 2026-08-20, zgloszenie klienta] Zapis jest ATOMOWY i z KOPIA ZAPASOWA.
    /// PRZED: <c>File.WriteAllText(path, "")</c> w <c>SettingsManager.RewriteSetting</c> kasowalo plik,
    /// a dopiero potem serializacja go odtwarzala. Kazde ubicie procesu w tym oknie (restart Windows
    /// przy aktualizacji, <c>Environment.Exit</c> Velopacka, crash, wyjatek serializacji) zostawialo
    /// plik PUSTY - a pusty plik jest przy starcie cicho traktowany jak "brak ustawien" i podmieniany
    /// wartosciami domyslnymi (klient: cennik przeskoczyl z kg na m2 i z procesu na sam detal).
    /// TERAZ: (1) serializujemy do pamieci - blad NIE dotyka pliku na dysku, (2) piszemy do pliku
    /// tymczasowego, (3) podmieniamy jednym ruchem systemu plikow, zachowujac poprzednia wersje jako
    /// <c>.bak</c>. W kazdej chwili na dysku jest kompletny plik - stary albo nowy, nigdy pusty.
    /// </summary>
    /// <param name="JSONFileFullPath"></param>
    /// <param name="objectToBeWritten"></param>
    public static void WriteObjectToJSONFile(string JSONFileFullPath, object objectToBeWritten) {
        JsonSerializerSettings settings = CreateSerializerSettings();
        // Serializacja PRZED dotknieciem pliku: gdy rzuci (np. cykl referencji), na dysku zostaje
        // nietknieta poprzednia wersja ustawien zamiast pustki.
        string serializedObject = JsonConvert.SerializeObject(objectToBeWritten, Formatting.Indented, settings);

        WriteTextAtomically(JSONFileFullPath, serializedObject);
    }

    /// <summary>Sciezka kopii zapasowej dla danego pliku ustawien (poprzednia UDANA wersja zapisu).</summary>
    public static string GetBackupPath(string JSONFileFullPath) => JSONFileFullPath + ".bak";

    /// <summary>
    /// Zapisuje tekst tak, by plik docelowy NIGDY nie byl widziany w stanie czesciowym:
    /// zapis do <c>.tmp</c> + <c>File.Replace</c> (podmiana w jednym kroku systemu plikow,
    /// poprzednia zawartosc laduje w <c>.bak</c>). Gdy pliku docelowego jeszcze nie ma -
    /// zwykle <c>File.Move</c> (nie ma czego podmieniac ani z czego robic kopii).
    /// </summary>
    private static void WriteTextAtomically(string fullPath, string content) {
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        string tempPath = fullPath + ".tmp";
        File.WriteAllText(tempPath, content);

        if (!File.Exists(fullPath)) {
            File.Move(tempPath, fullPath);
            return;
        }

        try {
            // Replace = podmiana + kopia poprzedniej wersji. ignoreMetadataErrors:true, bo roznice
            // w atrybutach/ACL (plik w %PROGRAMDATA% tworzony przez inne konto Windows) nie moga
            // wywrocic zapisu ustawien.
            File.Replace(tempPath, fullPath, GetBackupPath(fullPath), ignoreMetadataErrors: true);
        } catch (IOException) {
            // Replace potrafi odmowic tam, gdzie zwykle kopiowanie przechodzi (chwilowa blokada pliku
            // przez antywirusa, udzial sieciowy, system plikow bez wsparcia dla podmiany). Kopiujemy
            // wtedy GOTOWA tresc z pliku tymczasowego - nadal nigdy nie kasujemy pliku "na zapas",
            // wiec najgorszy przypadek to stara zawartosc, a nie pustka.
            FallbackCopy(tempPath, fullPath);
        } catch (UnauthorizedAccessException) {
            FallbackCopy(tempPath, fullPath);
        }
    }


    /// <summary>Awaryjna sciezka zapisu: kopiuje gotowa tresc z pliku tymczasowego i sprzata po sobie.
    /// Kopia zapasowa jest robiona przed nadpisaniem, zeby nie stracic poprzedniej wersji.</summary>
    private static void FallbackCopy(string tempPath, string fullPath) {
        try { File.Copy(fullPath, GetBackupPath(fullPath), overwrite: true); } catch { }
        File.Copy(tempPath, fullPath, overwrite: true);
        try { File.Delete(tempPath); } catch { }
    }

    /// <summary>
    /// If JSON is empty then returns an empty object. Otherwise it returns deserialized JSON as object
    /// </summary>
    /// <typeparam name="ObjectType"></typeparam>
    /// <param name="JSONFullFilePath"></param>
    /// <returns></returns>
    public static ObjectType DeserializeJSON<ObjectType>(string JSONFullFilePath) {
        string deserializedJSON = File.ReadAllText(JSONFullFilePath);
        if (deserializedJSON.Length > 0) {
            JsonSerializerSettings settings = CreateSerializerSettings();
            return JsonConvert.DeserializeObject<ObjectType>(deserializedJSON, settings)!;
        }
        return (ObjectType)Activator.CreateInstance<ObjectType>();
    }

    /// <summary>
    /// [Utrata ustawien 2026-08-20] Proba odczytu KONKRETNEGO pliku BEZ podstawiania wartosci domyslnych.
    /// Zwraca false gdy plik nie istnieje, jest pusty (typowy skutek przerwanego zapisu ze starej wersji),
    /// zawiera same biale znaki albo nie daje sie zdeserializowac. Dzieki temu wolajacy moze rozroznic
    /// "user nigdy tego nie ustawial" od "ustawienia zostaly utracone" - to drugie jest awaria, nie stanem
    /// normalnym, i ma trafic do telemetrii zamiast po cichu nadpisac config domyslnymi wartosciami.
    /// </summary>
    public static bool TryDeserializeJSON<ObjectType>(string JSONFullFilePath, out ObjectType? result, out string? failureReason) {
        result = default;
        failureReason = null;

        if (!File.Exists(JSONFullFilePath)) {
            failureReason = "plik nie istnieje";
            return false;
        }

        string raw;
        try {
            raw = File.ReadAllText(JSONFullFilePath);
        } catch (Exception ex) {
            failureReason = "blad odczytu: " + ex.GetType().Name + " - " + ex.Message;
            return false;
        }

        if (string.IsNullOrWhiteSpace(raw)) {
            failureReason = "plik pusty (" + raw.Length + " znakow)";
            return false;
        }

        try {
            JsonSerializerSettings settings = CreateSerializerSettings();
            var deserialized = JsonConvert.DeserializeObject<ObjectType>(raw, settings);
            if (deserialized is null) {
                failureReason = "deserializacja zwrocila null";
                return false;
            }
            result = deserialized;
            return true;
        } catch (Exception ex) {
            failureReason = "blad deserializacji: " + ex.GetType().Name + " - " + ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Creates JSON file and its directory if they are not exist.
    /// </summary>
    /// <param name="JSONFullFilePath"></param>
    public static void CreateJSONFileAndItsDirectory(string JSONFullFilePath) {
        Directory.CreateDirectory(Path.GetDirectoryName(JSONFullFilePath)!);    //Creates directory if not exist
        if (!File.Exists(JSONFullFilePath)) {    // Check if file already exists
            using (FileStream fs = File.Create(JSONFullFilePath)) {		// If doesn't exist then create the file
            }
        }
    }
}
