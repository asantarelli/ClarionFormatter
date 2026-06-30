# Changelog

## [2.0.0] - 2026-06-30

### Rediseño completo — solo IA

- Eliminado el formatter clásico (reglas hardcodeadas). El único modo es vía Claude API.
- Configuración simplificada: una sola pantalla con API Key, modelo, protocolo e instrucciones adicionales.
- **Perfiles por proyecto**: cada perfil apunta a su propio archivo de protocolo `.md`.
- **Fallback automático**: si el perfil no tiene protocolo configurado, busca `Protocolo_WindowFormatter.md` en `%APPDATA%\ClarionAssistant\`.
- Botón "Editar protocolo" en la configuración para abrir el `.md` directamente desde el IDE.
- Modelo predeterminado cambiado a `claude-sonnet-4-6` (mejor calidad).
- **Bugfix:** el `END` final del bloque `WINDOW` aparecía duplicado como `ENDD` al aplicar el resultado.

## [1.1.0] - 2026-06-29

### Integración con Claude API

- Nuevo comando: `Formatear ventana con IA (Claude)...`
- Soporte de archivo de protocolo `.md` configurable por perfil.
- Diálogo de progreso con cancelación durante la llamada a la API.
- Manejo de errores de API con mensajes descriptivos.
- TLS 1.2 forzado para compatibilidad con .NET Framework en Clarion IDE.

## [1.0.0] - 2026-06-28

### Versión inicial

- Formatter clásico basado en perfiles de configuración numérica.
- Perfiles con coordenadas, alturas, colores y flags configurables por el usuario.
- Soporte de múltiples perfiles nombrados.
- Comando `Aplicar ventana desde clipboard` para aplicar resultados de IA pegados manualmente.
