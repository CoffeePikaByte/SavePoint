SavePoint 
    SavePoint es un sistema en el que puedes tener tus videojuegos de   diferentes plataformas en un solo lugar. Ademas SavePoint te permite vizualizar datos como tiempo aprox de duracion del videojuego, tus logros, calificacion en metacritic y compartir publicaciones o guias de tus videojuegos favoritos.

Vision del proyecto
    El objetivo de SavePoint es solucionar el problema de tener datos de tus juegos dispersos en diferentes plataformas, ademas de darte una oportunidad de compartir acerca de tus juegos favoritos con amigos y otros gamers.

MVP (Minimum Viable Product)
    Funcionalidades iniciales del sistemas:
        ·Registro de usuario y login
        ·Busqueda de videojuegos (API Externa)
        ·Agregar videojuegos a biblioteca personal
        ·Vizualizar tu propia biblioteca
        ·Cambiar estado del videojuego
            ·Pendiente
            ·En proceso
            ·Terminado
            ·Abandonado

Fuera de alcance (Por ahora)
    ·Sistema de amigos o red social
    ·Chat entre usuarios
    ·Logros o trofeos
    ·Integración con Steam / PlayStation / Xbox
    ·Recomendaciones avanzadas con IA

Arquitectura del proyecto
    El proyecto sigue una estructura de Clean Arquitecture:
        ·GameHub.Domain → Entidades del negocio
        ·GameHub.Application → Casos de uso y lógica de aplicación
        ·GameHub.Infrastructure → Base de datos y servicios externos
        ·GameHub.API → Endpoints REST

Tecnologias utilizadas en el proyecto
    · .NET 8 Web API
    · Entity Framework Core
    · PostgreSQL
    · React + TypeScript (frontend)
    · JWT Authentication
    · Docker (más adelante)

Estado del proyecto
    Actualmente el backend dispone de autenticación, catálogo local y la operación de incorporación a la biblioteca descrita a continuación.

Biblioteca personal: agregar un juego
    POST /api/library/games
    Authorization: Bearer <JWT válido>
    Content-Type: application/json
    Cuerpo: { "gameId": "<GUID del catálogo local>" }

    El usuario se obtiene exclusivamente del JWT, no del cuerpo. Deben existir tanto el usuario como el juego local. Esta operación no consulta RAWG ni importa juegos.
    Respuesta 201: { "id": "<GUID>", "gameId": "<GUID>", "status": 0, "hoursPlayed": null, "addedAt": "<fecha UTC>" }.
    El estado inicial 0 corresponde a BackLog. No se incluyen entidades de navegación ni una URL de consulta aún no implementada.
    Errores: 400 para gameId ausente o inválido; 401 para JWT ausente, inválido o vencido, o identidad ausente/inválida; 404 para usuario o juego inexistente; 409 si el mismo usuario ya tiene el juego.
    El índice único (UserId, GameId) ya figura en la migración inicial. El repositorio traduce únicamente la violación de ese índice a conflicto; otros errores mantienen el manejo habitual.

    Build y pruebas rápidas (sin PostgreSQL):
        dotnet build GameHub.sln
        dotnet test GameHub.Application.Tests/GameHub.Application.Tests.csproj
        dotnet test GameHub.API.Tests/GameHub.API.Tests.csproj
    Para integración real con PostgreSQL, usar exclusivamente el script habilitado explícitamente:
        SAVEPOINT_POSTGRES_TESTS=1 bash scripts/test-postgres.sh
    Los tests HTTP usan JWT firmados con claves de prueba y repositorios simulados. Las pruebas del repositorio simulan errores del proveedor: no conectan a PostgreSQL.
    La suite de integración descrita a continuación verifica persistencia, restricciones y concurrencia del repositorio contra PostgreSQL 16 real. No constituye una prueba HTTP concurrente de extremo a extremo.

Pruebas de integración de PostgreSQL
    Requisitos: .NET SDK compatible con net8.0, Bash, Python 3, Docker Engine local en unix:///var/run/docker.sock y Docker Compose v2 con soporte para --wait. No se admiten motores remotos. El usuario debe tener acceso autorizado al socket; no se ejecuta sudo automáticamente.

    Ejecutar desde la raíz del repositorio:
        python3 -B scripts/test_postgres_safety.py
        SAVEPOINT_POSTGRES_TESTS=1 bash scripts/test-postgres.sh

    El script usa exclusivamente docker-compose.integration.yml, un nombre de proyecto aleatorio, puerto aleatorio en 127.0.0.1 y datos en tmpfs. No combina el Compose de desarrollo, no monta archivos del proyecto ni utiliza su contenedor, credenciales o volúmenes. La imagen PostgreSQL 16 está fijada por digest; si ya existe, no se actualiza ni elimina. Una referencia descargada por esta ejecución solo se elimina tras verificar identidad y ausencia de contenedores que la utilicen.

    SAVEPOINT_POSTGRES_TESTS=1 es una habilitación obligatoria, no una conexión. El script genera SAVEPOINT_TEST_CONNECTION, SAVEPOINT_TEST_RUN_ID y SAVEPOINT_TEST_TOKEN; rechaza valores proporcionados por el llamador. No hay alternativa basada en appsettings, secretos de usuario ni conexión de desarrollo. Las claves temporales no se imprimen. La configuración de Docker se aísla para no reutilizar credenciales de registros ni contextos locales.

    Cada caso crea una base con nombre generado, verifica el marcador de propiedad de la ejecución y aplica las dos migraciones reales mediante MigrateAsync(), nunca EnsureCreated(). Después siembra datos ficticios. Las conexiones no usan pooling; cada lectura o escritura independiente utiliza su propio DbContext. Las fechas de comparación usan UTC y precisión compatible con PostgreSQL.

    Los casos cubren escritura y lectura con otro contexto, consultas limitadas al usuario, ausencia de resultados, ambas claves foráneas, duplicado del índice específico, otro conflicto único (clave primaria), usuarios distintos y concurrencia. La carrera sincroniza dos contextos tras comprobar ausencia y espera una inserción y una GameAlreadyInLibraryException con el error PostgreSQL correcto. Timeouts y otros fallos no se consideran conflictos exitosos. Un timeout de espera no cancela los workers: el test espera su terminación antes de disponer la base y conserva el timeout como fallo. Los comandos tienen límites de conexión, SQL y bloqueo; si el proveedor queda bloqueado pese a ellos, el script impone un límite final de 300 segundos al proceso de tests y destruye exclusivamente su entorno.

    Limpieza: se cierran los contextos, se elimina cada base exclusivamente después de verificar su nombre y marcador, y se comprueba que no queden bases por prueba. En finally el script verifica etiquetas del proyecto y de la ejecución antes de usar down --volumes --remove-orphans sobre ese proyecto exacto. Comprueba que no queden contenedores, redes o volúmenes y elimina sus archivos temporales. No ejecuta comandos globales de limpieza.

    Si falla la identidad de un recurso, el script se detiene sin eliminarlo. Un SIGKILL, fallo del daemon o corte de energía puede impedir la limpieza. El script imprime el proyecto spit_<identificador>; antes de una recuperación manual se deben inspeccionar ambos labels: com.docker.compose.project e io.savepoint.integration.run. No usar el Compose de desarrollo, docker system prune ni eliminar recursos por un prefijo amplio.

    La suite falla, sin omitir pruebas, si se ejecuta directamente sin habilitación o conexión generada. Por eso dotnet test GameHub.sln sin el script no es el comando habitual de pruebas rápidas. Después de la integración y su limpieza, ejecutar:
        dotnet build GameHub.sln
        dotnet test GameHub.Application.Tests/GameHub.Application.Tests.csproj
        dotnet test GameHub.API.Tests/GameHub.API.Tests.csproj

    Los resultados de integración se muestran individualmente en consola. El script devuelve un código distinto de cero si fallan las pruebas o la limpieza. Sus artefactos se generan fuera del repositorio y se destruyen al terminar. Las advertencias nullable preexistentes no forman parte de esta iteración.

Casos de uso del MVP
    ·Registrarse
    ·Iniciar sesion
    ·Agregar videojuegos a su libreria personal
    ·Buscar videojuegos
    ·Ver su biblioteca
    ·Cambiar de estados de los videojuegos de su biblioteca
    ·Eliminar un juego de su biblioteca

Entidades y sus relaciones del MVP
    User
        Usuario que puede registrarse e iniciar sesion.
    Game
        Videojuego el la registrado en el sistema.
    GameUser
        Los usuarios pueden tener cero, uno o multiple videojuegos agregados y los videojuegos pueden tener cero, uno o multiples usuarios que lo agregaron. 