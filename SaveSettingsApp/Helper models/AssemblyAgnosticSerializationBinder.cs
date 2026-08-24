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
/// ZAPIS (BindToName) zostaje domyslny - nowe pliki dostaja AKTUALNA nazwe assembly, wiec sam plik
/// "naprawia sie" przy pierwszym zapisie. Uwaga: starszy build aplikacji (bez tego bindera) nie odczyta
/// pliku zapisanego przez nowszy - rollback wersji aplikacji oznacza reset ustawien (akceptowane;
/// aktualizacje Velopack sa w praktyce jednokierunkowe).
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
