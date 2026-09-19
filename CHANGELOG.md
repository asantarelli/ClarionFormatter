# Changelog

## [2.1.1] - 2026-09-19

### Atribución y configuración propia

- `LICENSE` incluye el aviso de copyright y licencia MIT de Clarion Assistant (ClarionLive), del que deriva `Services/EditorService.cs`. El archivo lleva además una cabecera con el origen, y el README lo menciona.
- El manifiesto `.addin` declara `author="asantarelli"` (antes decía `ClarionAssistant`).
- La configuración se guarda en `%APPDATA%\ClarionWindowFormatter\` en lugar de `%APPDATA%\ClarionAssistant\`. La primera vez que se abre, el addin mueve `window-formatter.json` de la carpeta anterior, así que no se pierden perfiles ni la API key.

## [2.1.0] - 2026-09-19

### Reglas por tipo de control

- **Reglas estructuradas** en lugar del archivo de protocolo `.md`: la pestaña **Controles** permite definir, por tipo de control, Y base, incremento Y, X de etiqueta/control, altura, anchos, color, generación de TIP (con plantilla `{LABEL}`) y reglas adicionales en texto libre.
- Botón **Importar protocolo v2.5 como valores por defecto** para cargar un juego de reglas completo.
- **Formateo en dos pasadas**: paso 1 reformatea, paso 2 verifica el resultado contra las reglas y corrige. Si el paso 2 no devuelve un bloque válido, se usa el del paso 1.
- Configuración reorganizada en pestañas: Controles, Notas adicionales, API Claude.

### Cambios incompatibles

- Se eliminó el soporte de archivo de protocolo `.md` (`AiProtocolFile`) y el fallback a `%APPDATA%\ClarionAssistant\Protocolo_WindowFormatter.md`. Los perfiles existentes conservan nombre e instrucciones adicionales; las reglas deben cargarse en la pestaña Controles.

## [2.0.1] - 2026-07-03

### Bugfix

- Menú del IDE mostraba opciones eliminadas (formatter clásico, clipboard) porque el `.addin` deployado se generaba desde una copia en `bin\Debug\` en lugar de directamente desde el template.
- `deploy.ps1` corregido: el `.addin` ahora se copia directamente desde el template al destino, sin pasar por `bin\Debug\`.

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
