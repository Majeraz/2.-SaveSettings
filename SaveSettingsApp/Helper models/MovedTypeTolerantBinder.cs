using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace JSONFilesManagerProj;

/// <summary>
/// [Przenosiny typów między bibliotekami 2026-08-24] Rozpoznawanie typu przy odczycie JSON-a ODPORNE na to,
/// że klasa przeprowadziła się do innej biblioteki (.dll).
///
/// <para><b>Skąd to się wzięło:</b> pliki ustawień zapisujemy z <c>TypeNameHandling.All</c>, więc w JSON-ie
/// przy każdym obiekcie siedzi wpis <c>"$type": "Pełna.Nazwa.Klasy, NazwaBiblioteki"</c>. Domyślny mechanizm
/// Newtonsofta szuka klasy WYŁĄCZNIE w bibliotece wypisanej w pliku. Gdy klasa zostanie przeniesiona do innej
/// biblioteki (u nas: wydzielenie wspólnego backendu 2026-08-23, <c>UserModel</c> wyszedł z „DXF Manager” do
/// „DXFManagerBackend”), stary plik na dysku wskazuje bibliotekę, w której tej klasy już nie ma, i CAŁY plik
/// ustawień przestaje się dać odczytać:
/// <c>Could not find type '...UserModel' in assembly 'DXF Manager'</c>.</para>
///
/// <para><b>Dlaczego to jest groźne:</b> plik ustawień u klienta został zapisany PRZED aktualizacją, więc
/// awaria dotyczy KAŻDEJ istniejącej instalacji, nie tylko maszyny dewelopera. U nas wywala odczyt
/// <c>AppSettingsSharedServiceAppSettings.json</c>, czyli m.in. zalogowanego użytkownika i ścieżkę do
/// wspólnego folderu firmy.</para>
///
/// <para><b>Co robi ta klasa:</b> najpierw próbuje standardowego rozpoznania (biblioteka z pliku - zero zmian
/// zachowania dla plików, które są w porządku). Dopiero gdy tam typu NIE MA, szuka klasy o tej samej pełnej
/// nazwie w bibliotekach już wczytanych do procesu, a potem w bibliotekach, do których odwołuje się program.
/// Nazwa biblioteki z pliku staje się więc podpowiedzią, a nie warunkiem koniecznym. Typy ogólne
/// (np. <c>List&lt;LabelElement&gt;</c>) rozwiązuje ten sam mechanizm, bo zapytanie idzie przez
/// <see cref="Type.GetType(string, Func{AssemblyName, Assembly?}, Func{Assembly?, string, bool, Type?}, bool)"/>,
/// który woła nas osobno dla każdego składnika nazwy.</para>
///
/// <para><b>Zapis zostaje bez zmian</b> - do pliku nadal idzie biblioteka, w której klasa siedzi w tej chwili.
/// Gdyby zapisywać bez nazwy biblioteki, starsze wersje programu (a w firmie bywa kilka stanowisk w różnych
/// wersjach) nie odczytałyby takiego pliku w ogóle.</para>
/// </summary>
public sealed class MovedTypeTolerantBinder : ISerializationBinder {

    /// <summary>Jedna instancja na proces - wynik rozpoznania jest zapamiętywany, więc warto ją współdzielić.</summary>
    public static readonly MovedTypeTolerantBinder Instance = new();

    private static readonly DefaultSerializationBinder Default = new();
    private static readonly Dictionary<string, Type> Cache = new();
    private static readonly object CacheLock = new();

    /// <summary>Zapis: dokładnie to, co robił Newtonsoft do tej pory (pełna nazwa + bieżąca biblioteka).</summary>
    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName) =>
        Default.BindToName(serializedType, out assemblyName, out typeName);

    public Type BindToType(string? assemblyName, string typeName) {
        string key = (assemblyName ?? "") + "|" + typeName;
        lock (CacheLock)
            if (Cache.TryGetValue(key, out Type? cached)) return cached;

        Type? resolved = null;

        // 1) Standardowa droga - biblioteka wypisana w pliku. Dla poprawnych plików kończy się tutaj.
        try {
            resolved = Default.BindToType(assemblyName, typeName);
        } catch (JsonSerializationException) {
            // Typu nie ma tam, gdzie mówi plik - lecimy dalej, do szukania po nazwie.
        }

        // 2) Ta sama pełna nazwa klasy, ale w innej bibliotece (typ się przeprowadził).
        if (resolved is null) {
            string qualified = string.IsNullOrEmpty(assemblyName) ? typeName : typeName + ", " + assemblyName;
            resolved = Type.GetType(qualified, ResolveAssembly, ResolveType, throwOnError: false);
        }

        if (resolved is null)
            throw new JsonSerializationException(
                $"Nie znaleziono typu '{typeName}'" +
                (string.IsNullOrEmpty(assemblyName) ? "" : $" (plik wskazuje bibliotekę '{assemblyName}')") +
                " - ani we wskazanej bibliotece, ani w żadnej innej wczytanej do programu.");

        lock (CacheLock) Cache[key] = resolved;
        return resolved;
    }

    /// <summary>Biblioteka po nazwie: najpierw już wczytane do procesu, potem próba dociągnięcia z dysku.
    /// Null = nie znaleziono, wtedy <see cref="ResolveType"/> dostanie null i poszuka po samej nazwie klasy.</summary>
    private static Assembly? ResolveAssembly(AssemblyName name) {
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
            if (string.Equals(loaded.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase))
                return loaded;
        try {
            return Assembly.Load(new AssemblyName(name.Name!));
        } catch {
            return null;   // biblioteki o tej nazwie już nie ma - typ pewnie się przeprowadził
        }
    }

    /// <summary>Klasa: najpierw we wskazanej bibliotece, a gdy jej tam nie ma - w pozostałych.</summary>
    private static Type? ResolveType(Assembly? assembly, string typeName, bool ignoreCase) {
        Type? direct = assembly?.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
        if (direct is not null) return direct;
        return FindByFullName(typeName, ignoreCase);
    }

    /// <summary>
    /// Szuka klasy o danej pełnej nazwie w bibliotekach wczytanych do procesu, a potem w tych, do których
    /// program się odwołuje (mogą być jeszcze niewczytane - .NET ładuje je dopiero przy pierwszym użyciu).
    /// </summary>
    private static Type? FindByFullName(string typeName, bool ignoreCase) {
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies()) {
            Type? found = SafeGetType(loaded, typeName, ignoreCase);
            if (found is not null) return found;
        }

        Assembly? entry = Assembly.GetEntryAssembly();
        if (entry is null) return null;

        foreach (AssemblyName referenced in entry.GetReferencedAssemblies()) {
            Assembly? assembly;
            try { assembly = Assembly.Load(referenced); } catch { continue; }
            Type? found = SafeGetType(assembly, typeName, ignoreCase);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>Odpytanie biblioteki o typ nie może wywrócić odczytu ustawień (biblioteka natywna, uszkodzona itp.).</summary>
    private static Type? SafeGetType(Assembly assembly, string typeName, bool ignoreCase) {
        try {
            return assembly.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
        } catch {
            return null;
        }
    }
}
