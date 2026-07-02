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
    Actualmente el proyecto se encuentra en proceso de diseño y de estructuradocion.

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