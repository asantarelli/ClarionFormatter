# ClarionFormatter

Addin para el IDE de **Clarion 11/12** que reformatea bloques `WINDOW` usando la API de Claude (Anthropic AI).

El addin lee un archivo de protocolo `.md` definido por el usuario y le pide a Claude que aplique esas reglas directamente sobre el código de la ventana abierta en el editor, sin necesidad de copiar y pegar.

---

## Características

- Reformatea el bloque `WINDOW...END` del archivo activo directamente en el editor
- Utiliza un **archivo de protocolo personalizable** (`.md`) que define las reglas de formato
- Soporte de **múltiples perfiles**: uno por proyecto, cada uno con su propio protocolo
- Fallback automático al archivo `Protocolo_WindowFormatter.md` en `%APPDATA%\ClarionAssistant\`
- Instrucciones adicionales por perfil (campo libre de texto)
- Compatible con Clarion 11 y Clarion 12

---

## Requisitos

- Clarion 11 o 12 instalado
- Cuenta en [Anthropic Console](https://console.anthropic.com) con créditos de API
- API Key de Anthropic (`sk-ant-...`)

---

## Instalación

1. Descargar `ClarionWindowFormatter.dll` y `ClarionWindowFormatter.addin` de [Releases](../../releases)
2. Copiar ambos archivos a:
   ```
   <ClarionRoot>\accessory\addins\ClarionWindowFormatter\
   ```
3. Reiniciar el Clarion IDE
4. El menú **Tools** mostrará las opciones:
   - `Formatear ventana con IA (Claude)...`
   - `Formatear ventana - Configuracion...`

---

## Configuración

Ir a **Tools → Formatear ventana - Configuracion...**

| Campo | Descripción |
|-------|-------------|
| **API Key** | Tu clave de Anthropic (global, todos los perfiles) |
| **Modelo** | Modelo de Claude a usar (recomendado: `claude-sonnet-4-6`) |
| **Archivo de protocolo** | Ruta al `.md` con tus reglas de formato (por perfil) |
| **Instrucciones adicionales** | Reglas extra en texto libre (por perfil) |

### Archivo de protocolo

El archivo de protocolo es un `.md` de texto libre que describe las reglas que Claude debe aplicar al formatear la ventana. Incluye un [ejemplo completo](protocolo/Protocolo_WindowFormatter.md) en este repositorio.

Si no configurás un archivo de protocolo, el addin busca automáticamente:
```
%APPDATA%\ClarionAssistant\Protocolo_WindowFormatter.md
```

---

## Uso

1. Abrir un archivo `.clw` en el editor del IDE con un bloque `WINDOW`
2. Ir a **Tools → Formatear ventana con IA (Claude)...**
3. Claude reformatea la ventana y aplica el resultado directamente en el editor
4. Revisar el resultado y guardar con `Ctrl+S`

> **Nota:** El addin reemplaza el bloque `WINDOW...END` completo. Se recomienda tener el archivo bajo control de versiones antes de aplicar.

---

## Compilar desde fuente

Requiere Visual Studio 2019 o superior con .NET Framework 4.7.2.

```bash
msbuild ClarionWindowFormatter\ClarionWindowFormatter.csproj /p:Configuration=Debug /p:ClarionRoot="D:\Clarion11"
```

---

## Licencia

MIT License — ver [LICENSE](LICENSE)

---

## Contribuciones

Los PRs son bienvenidos. Si tenés un archivo de protocolo interesante para compartir, podés agregarlo en la carpeta `protocolo/`.
