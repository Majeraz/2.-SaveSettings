using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace JSONFilesManagerProj;

/// <summary>
/// [Przenosiny typow miedzy assembly 2026-08-24] Binder typow dla TypeNameHandling.All ODPORNY na
/// przeprowadzke klasy do innej biblioteki.
///
/// PROBLEM, ktory rozwiazuje: pliki ustawien maja zapisane pelne nazwy typow RAZEM z nazwa assembly
/// (np. "DXF_Ffile_Analyse...UserModel, DXF Manager"). Ekstrakcja wspolnego backendu (E2, 2026-08-23)
/// przeniosla te klasy do assembly "DXFManagerBackend" - namespace bez zmian, ale STARY JSON dalej
/// wskazuje stare assembly, wiec domyslny binder rzucal "Could not find type ... in assembly 'DXF Manager'"
/// i ustawienia byly traktowane jak uszkodzone (reset + wylogowanie u kazdego klienta po aktualizacji).
///
/// ROZWIAZANIE: najpierw sciezka domyslna (100% dotychczasowego zachowania dla poprawnych plikow);
/// dopiero gdy ona NIE znajduje typu, szukamy typu po PELNEJ NAZWIE (namespace+klasa) we wszystkich
/// JUZ ZALADOWANYCH assembly procesu. Obsluguje tez nazwy zagniezdzone w generykach
/// (np. List`1[[UserModel, DXF Manager]]) przez Type.GetType z wlasnymi resolverami.
///
/// BEZPIECZENSTWO: fallback NIE laduje zadnych nowych assembly z dysku (przeszukuje wylacznie
/// AppDomain.CurrentDomain.GetAssemblies()) - powierzchnia jest WEZSZA niz domyslnego bindera,
/// ktory potrafi doladowac assembly po nazwie.
///
/// ZAPIS (BindToName) - [ZMIANA 2026-08-25, obserwacja z maszyny dev, nie teoria]: dla typow przeniesionych
/// do nowej biblioteki emitujemy STARA nazwe assembly (mapa LegacyAssemblyNames), nie aktualna. Powod: plik
/// zapisany z nowa nazwa ("UserModel, DXFManagerBackend") wywracal STARSZY build (4.2.13) TWARDYM CRASHEM na
/// starcie - bez okna i bez komunikatu (unhandled JsonSerializationException w SettingsManager.GetSetting;
/// 4 proby uruchomienia z pulpitu = 4 wpisy APPCRASH w dzienniku zdarzen Windows). Wczesniejsze zalozenie
/// "starszy build nie odczyta = reset ustawien (akceptowane)" bylo bledne: starsze buildy NIE MAJA
/// LoadWithRecovery, wiec zamiast resetu jest smierc procesu - a aplikacja ma mechanizm cofania wersji po
/// nieudanym starcie, czyli rollback prowadzil klienta w martwy punkt. Po tej zmianie plik jest czytelny
/// W OBIE STRONY: stary build znajduje typ pod stara nazwa (u niego klasa naprawde tam lezy), a nowy build
/// czyta go fallbackiem z BindToType wyzej - ta sama sciezka, ktora i tak obsluguje pliki sprzed ekstrakcji.
/// </summary>
public sealed class AssemblyAgnosticSerializationBinder : DefaultSerializationBinder {

    public override Type BindToType(string? assemblyName, string typeName) {
        try {
            return base.BindToType(assemblyName, typeName);
        } catch (JsonSerializationException) {
            // Typ nieznajdowalny w zapisanym assembly - typowy skutek przeniesienia klasy
            // do innej biblioteki. Probujemy znalezc go po samej pelnej nazwie.
        }

        // Zloz pelna nazwe tak, jak widzi ja Type.GetType; resolvery obsluguja i typ zewnetrzny,
        // i nazwy zagniezdzone w argumentach generykow.
        // PULAPKA Type.GetType: gdy nazwa zawiera assembly, a assemblyResolver zwroci null,
        // typeResolver NIE jest wolany w ogole (rozwiazywanie konczy sie od razu). Dlatego dla
        // nieznanego assembly zwracamy ZASTEPCZE (ta biblioteka) - typeResolver i tak przeszukuje
        // wszystkie zaladowane assembly, gdy typu nie ma w dostarczonym.
        string composedName = assemblyName is null ? typeName : typeName + ", " + assemblyName;
        Type? resolved = Type.GetType(
            composedName,
            static asmName => FindLoadedAssembly(asmName) ?? typeof(AssemblyAgnosticSerializationBinder).Assembly,
            static (assembly, name, ignoreCase) =>
                assembly?.GetType(name, throwOnError: false, ignoreCase: ignoreCase)
                ?? FindTypeInLoadedAssemblies(name, ignoreCase),
            throwOnError: false);

        if (resolved is not null)
            return resolved;

        throw new JsonSerializationException(
            $"Nie znaleziono typu '{typeName}' (zapisane assembly: '{assemblyName}') w zadnym zaladowanym assembly. " +
            "Typ zostal usuniety/przemianowany albo biblioteka z nim nie jest zaladowana.");
    }

    /// <summary>
    /// [Wsteczna zgodnosc zapisu 2026-08-25] Assembly, ktorych typy przeniosla ekstrakcja backendu (E2)
    /// -> nazwa assembly, pod ktora te same typy zna KAZDY starszy build. Kazda kolejna ekstrakcja
    /// biblioteki z exe MUSI dopisac tu swoj wpis - inaczej starsze buildy przestana czytac wspolne pliki
    /// ustawien, i to nie resetem, tylko crashem na starcie (patrz komentarz klasy).
    /// </summary>
    private static readonly Dictionary<string, string> LegacyAssemblyNames = new() {
        ["DXFManagerBackend"] = "DXF Manager",
    };

    /// <summary>
    /// ZAPIS: typ przeniesiony do nowej biblioteki dostaje w JSON STARA nazwe assembly - patrz komentarz
    /// klasy. Nazwy pozostalych assembly zostaja bez zmian.
    /// </summary>
    public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
        base.BindToName(serializedType, out assemblyName, out typeName);
        assemblyName = RewriteToLegacyAssemblyName(assemblyName);
        // Typy generyczne (List<UserModel> itp.) maja nazwy assembly argumentow ZAGNIEZDZONE w typeName
        // ("List`1[[UserModel, DXFManagerBackend, ...]]") - sama podmiana assemblyName by ich nie objela.
        typeName = RewriteGenericArgumentAssemblies(typeName);
    }

    private static string? RewriteToLegacyAssemblyName(string? assemblyName) {
        if (assemblyName is null) return null;
        int comma = assemblyName.IndexOf(',');
        string simpleName = (comma < 0 ? assemblyName : assemblyName[..comma]).Trim();
        // Zwracamy sama prosta nazwe (bez Version/Culture) - dokladnie w tej formie stare pliki zapisywaly
        // assembly i dokladnie te forme rozumie kazdy build.
        return LegacyAssemblyNames.TryGetValue(simpleName, out string? legacyName) ? legacyName : assemblyName;
    }

    private static string? RewriteGenericArgumentAssemblies(string? typeName) {
        if (typeName is null || !typeName.Contains('[')) return typeName;
        foreach ((string movedAssembly, string legacyAssembly) in LegacyAssemblyNames) {
            typeName = typeName
                .Replace(", " + movedAssembly + ",", ", " + legacyAssembly + ",")
                .Replace(", " + movedAssembly + "]", ", " + legacyAssembly + "]");
        }
        return typeName;
    }

    /// <summary>Assembly o tej nazwie sposrod JUZ zaladowanych (bez ladowania z dysku). null = nieznalezione
    /// (wtedy resolver typu i tak przeszuka wszystkie zaladowane).</summary>
    private static Assembly? FindLoadedAssembly(AssemblyName asmName) {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (string.Equals(assembly.GetName().Name, asmName.Name, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }
        return null;
    }

    /// <summary>Typ o podanej pelnej nazwie (namespace+klasa) w KTORYMKOLWIEK zaladowanym assembly.
    /// Pierwsze trafienie wygrywa - nazwy typow ustawien sa unikalne w ramach procesu.</summary>
    private static Type? FindTypeInLoadedAssemblies(string fullTypeName, bool ignoreCase) {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type? type = assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: ignoreCase);
            if (type is not null)
                return type;
        }
        return null;
    }
}
