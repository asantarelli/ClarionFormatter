# Guía de uso — ClarionFormatter

## Índice

1. [Instalación](#instalación)
2. [Configuración inicial](#configuración-inicial)
3. [Uso cotidiano](#uso-cotidiano)
4. [El archivo de protocolo](#el-archivo-de-protocolo)
5. [Perfiles](#perfiles)
6. [Solución de problemas](#solución-de-problemas)
7. [Preguntas frecuentes](#preguntas-frecuentes)

---

## Instalación

### Paso 1 — Obtener una API Key de Anthropic

1. Crear una cuenta en [console.anthropic.com](https://console.anthropic.com)
2. Ir a **Billing** y cargar créditos (mínimo $5 USD)
3. Ir a **API Keys** → **Create Key**
4. Copiar la clave (empieza con `sk-ant-...`) — se muestra una sola vez

### Paso 2 — Instalar el addin

1. Descargar `ClarionWindowFormatter.dll` y `ClarionWindowFormatter.addin` desde [Releases](../../releases)
2. Crear la carpeta (si no existe):
   ```
   C:\Clarion11\accessory\addins\ClarionWindowFormatter\
   ```
3. Copiar los dos archivos descargados a esa carpeta
4. Reiniciar el IDE de Clarion

Verificar que aparezcan en **Tools**:
- `Formatear ventana con IA (Claude)...`
- `Formatear ventana - Configuracion...`

---

## Configuración inicial

Ir a **Tools → Formatear ventana - Configuracion...**

### API Key

Pegar la clave de Anthropic en el campo **API Key**. Se almacena en:
```
%APPDATA%\ClarionAssistant\window-formatter.json
```
No se envía a ningún lugar más que a la API de Anthropic.

### Modelo

El modelo predeterminado es `claude-sonnet-4-6`, que ofrece la mejor calidad para esta tarea.

| Modelo | Velocidad | Calidad | Costo |
|--------|-----------|---------|-------|
| `claude-sonnet-4-6` | Normal (~10s) | Alta | Medio |
| `claude-haiku-4-5-20251001` | Rápido (~3s) | Media | Bajo |

### Archivo de protocolo

Hacer clic en `...` para seleccionar tu archivo `.md` de protocolo, o escribir la ruta directamente.

Si no configurás ninguno, el addin busca automáticamente:
```
%APPDATA%\ClarionAssistant\Protocolo_WindowFormatter.md
```
Podés copiar el [ejemplo incluido](protocolo/Protocolo_WindowFormatter.md) a esa ubicación como punto de partida.

---

## Uso cotidiano

1. Abrir el archivo `.clw` en el editor del IDE
2. Asegurarse de que el cursor esté dentro del bloque `WINDOW...END`
3. Ir a **Tools → Formatear ventana con IA (Claude)...**
4. Esperar la respuesta de Claude (aparece un diálogo de progreso con botón Cancelar)
5. Revisar el resultado en el editor
6. Si está conforme, guardar con `Ctrl+S`
7. Si no está conforme, deshacer con `Ctrl+Z`

> **Consejo:** Antes de formatear por primera vez una ventana importante, guardar el archivo (`Ctrl+S`) para tener un punto de restauración.

---

## El archivo de protocolo

El archivo de protocolo es el corazón del addin. Es un archivo de texto (`.md`) que contiene las reglas que Claude debe seguir al reformatear la ventana.

### ¿Qué puede incluir?

- Reglas de coordenadas (posición Y de la primera fila, espaciado entre filas, margen X)
- Reglas de alturas de controles
- Esquema de colores semántico
- Reglas de tooltips
- Reglas de limpieza de código (qué conservar, qué eliminar)
- Reglas específicas por tipo de ventana (formularios, browses)
- Ejemplos de antes/después
- Cualquier otra convención de tu equipo

### ¿Cómo editarlo?

Desde la configuración del addin, hacer clic en **Editar protocolo** — esto abre el archivo en el editor predeterminado del sistema.

También podés editarlo con cualquier editor de texto: Notepad, VS Code, Notepad++, etc.

Los cambios se aplican en el próximo formateo, sin reiniciar el IDE.

### Ejemplo incluido

El archivo [protocolo/Protocolo_WindowFormatter.md](protocolo/Protocolo_WindowFormatter.md) es un ejemplo completo basado en el Protocolo Clarion v2.5, con reglas para:
- Formularios de entrada de datos
- Ventanas de tipo Browse
- Colores semánticos
- Tooltips descriptivos
- Dimensiones de ventana

---

## Perfiles

Los perfiles permiten tener diferentes configuraciones para diferentes proyectos.

### Crear un perfil nuevo

1. Abrir la configuración
2. Hacer clic en **Nuevo**
3. Asignar un nombre (ej: "Proyecto ABC")
4. Seleccionar el archivo de protocolo correspondiente

### Cambiar de perfil

Seleccionar el perfil deseado en el combo de la parte superior de la configuración y guardar.

### ¿Para qué usar perfiles?

- Distintos proyectos con distintas convenciones de formato
- Variantes de un mismo protocolo (ej: formularios simples vs. formularios complejos)
- Un perfil "estricto" y uno "permisivo" según el estado del proyecto

---

## Solución de problemas

### El menú no aparece en Tools

- Verificar que los archivos `.dll` y `.addin` estén en la carpeta correcta de addins
- Verificar que el nombre de la carpeta coincida con el nombre del addin
- Reiniciar completamente el IDE

### Error "API key no configurada"

- Ir a la configuración y verificar que la API Key esté cargada
- Verificar que la clave empiece con `sk-ant-`

### Error de API (401 Unauthorized)

- La API Key es incorrecta o fue revocada
- Generar una nueva desde [console.anthropic.com](https://console.anthropic.com)

### Error de API (529 / Overloaded)

- La API de Anthropic está temporalmente sobrecargada
- Reintentar en unos minutos

### Claude no encontró el bloque WINDOW

- Verificar que el archivo tenga un bloque `WINDOW...END` completo
- El archivo debe estar abierto y activo en el editor

### El resultado tiene errores de compilación en Clarion

- Revisar el archivo de protocolo — puede estar indicando cambios que no aplican al tipo de ventana
- Agregar instrucciones más precisas en **Instrucciones adicionales** del perfil
- Usar `Ctrl+Z` para deshacer y ajustar el protocolo

---

## Preguntas frecuentes

**¿Mis archivos de código se envían a Anthropic?**

Solo el bloque `WINDOW...END` del archivo activo se envía a la API junto con el contenido del archivo de protocolo. No se envía ningún otro código.

**¿Cuánto cuesta cada formateo?**

Depende del tamaño de la ventana y del protocolo. Aproximadamente:
- Con `claude-sonnet-4-6`: ~$0.002 a $0.010 por ventana
- Con `claude-haiku-4-5-20251001`: ~$0.0002 a $0.001 por ventana

**¿Puedo compartir mi archivo de protocolo con el equipo?**

Sí, es un archivo de texto plano. Podés versionarlo en tu repositorio de código o compartirlo por cualquier medio. Cada integrante del equipo lo configura en su perfil local.

**¿Funciona con Clarion 12?**

Sí, el addin es compatible con Clarion 11 y 12.
