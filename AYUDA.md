# Guía de uso — ClarionFormatter

## Índice

1. [Instalación](#instalación)
2. [Configuración inicial](#configuración-inicial)
3. [Uso cotidiano](#uso-cotidiano)
4. [Reglas de formato](#reglas-de-formato)
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

### Reglas de formato

En la pestaña **Controles**, hacer clic en **Importar protocolo v2.5 como valores por defecto** para cargar un juego de reglas completo como punto de partida. Después se pueden ajustar control por control (ver [Reglas de formato](#reglas-de-formato)).

---

## Uso cotidiano

1. Abrir el archivo `.clw` en el editor del IDE
2. Asegurarse de que el cursor esté dentro del bloque `WINDOW...END`
3. Ir a **Tools → Formatear ventana con IA (Claude)...**
4. Esperar la respuesta de Claude. El diálogo de progreso muestra dos pasos: *Reformateando ventana* y *Verificando resultado*. Se puede cancelar en cualquier momento
5. Revisar el resultado en el editor
6. Si está conforme, guardar con `Ctrl+S`
7. Si no está conforme, deshacer con `Ctrl+Z`

> **Consejo:** Antes de formatear por primera vez una ventana importante, guardar el archivo (`Ctrl+S`) para tener un punto de restauración.

---

## Reglas de formato

Las reglas se definen en la pestaña **Controles** de la configuración, una ficha por tipo de control: `WINDOW`, `PROMPT`, `ENTRY`, `TEXT`, `CHECK`, `OPTION`, `LIST`, `COMBO`, `BUTTON`, `STRING`, `IMAGE`, `GROUP`, `PANEL`, `SHEET`, `TAB`, `SPIN`.

### Campos de cada regla

| Campo | Qué indica |
|-------|------------|
| **Y base** | Y de la primera fila |
| **Incremento Y** | Separación vertical entre filas |
| **X etiqueta / X control** | Columna de las etiquetas y de los controles |
| **Altura, Ancho mín., Ancho máx.** | Dimensiones del control |
| **COLOR** | Atributo de color, ej. `COLOR(00E0F0FFh)` |
| **Generar TIP automáticamente** | Si se agrega `TIP()`. La plantilla admite `{LABEL}` |
| **Reglas adicionales** | Texto libre, una regla por línea (casos especiales, alineaciones, excepciones) |

Los campos vacíos se omiten. Si un control no tiene ningún valor cargado, no se envía.

### ¿Cómo se usa?

Al formatear, el addin arma un protocolo con las reglas del perfil activo y las **Notas adicionales**, y hace dos llamadas a Claude:

1. **Reformatear**: aplica las reglas al bloque `WINDOW`.
2. **Verificar**: revisa el resultado contra las mismas reglas y corrige incumplimientos.

En ambos pasos se le indica a Claude que conserve sin cambios `USE()`, `FORMAT()`, `MSG()`, `#SEQ()`, `#ORIG()`, `#ORDINAL()`, `#LINK()`, `#FIELDS()` y los textos, y que no agregue ni quite controles.

Los cambios en las reglas se aplican en el próximo formateo, sin reiniciar el IDE.

### Valores por defecto (Protocolo v2.5)

El botón **Importar protocolo v2.5 como valores por defecto** carga reglas basadas en el Protocolo Clarion v2.5: formularios de entrada, ventanas Browse, colores semánticos, tooltips y dimensiones de ventana. El documento original está en [protocolo/Protocolo_WindowFormatter.md](protocolo/Protocolo_WindowFormatter.md) como referencia.

> **Actualizando desde 2.0.x:** el archivo de protocolo `.md` ya no se usa. Los perfiles existentes conservan nombre e instrucciones adicionales, pero hay que cargar las reglas en la pestaña **Controles** (por ejemplo, importando el Protocolo v2.5).

---

## Perfiles

Los perfiles permiten tener diferentes configuraciones para diferentes proyectos.

### Crear un perfil nuevo

1. Abrir la configuración
2. Hacer clic en **Nuevo**
3. Asignar un nombre (ej: "Proyecto ABC")
4. Cargar las reglas en la pestaña **Controles**

### Cambiar de perfil

Seleccionar el perfil deseado en el combo de la parte superior de la configuración y guardar.

### ¿Para qué usar perfiles?

- Distintos proyectos con distintas convenciones de formato
- Variantes de un mismo juego de reglas (ej: formularios simples vs. formularios complejos)
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

- Revisar las reglas del control afectado — pueden estar indicando cambios que no aplican al tipo de ventana
- Agregar instrucciones más precisas en **Notas adicionales** del perfil
- Usar `Ctrl+Z` para deshacer y ajustar las reglas

---

## Preguntas frecuentes

**¿Mis archivos de código se envían a Anthropic?**

Solo el bloque `WINDOW...END` del archivo activo se envía a la API, junto con las reglas del perfil. No se envía ningún otro código.

**¿Cuánto cuesta cada formateo?**

Depende del tamaño de la ventana y de las reglas. Desde la versión 2.1 cada formateo hace dos llamadas (reformatear + verificar), así que el costo es aproximadamente el doble que en 2.0:
- Con `claude-sonnet-4-6`: ~$0.004 a $0.020 por ventana
- Con `claude-haiku-4-5-20251001`: ~$0.0004 a $0.002 por ventana

**¿Puedo compartir mis reglas con el equipo?**

Las reglas se guardan en `%APPDATA%\ClarionAssistant\window-formatter.json`. Se puede compartir ese archivo, pero **contiene la API Key**: borrarla antes de pasarlo.

**¿Funciona con Clarion 12?**

Sí, el addin es compatible con Clarion 11 y 12.
