# ClarionFormatter

Addin para el IDE de **Clarion 11/12** que reformatea bloques `WINDOW` usando la API de Claude (Anthropic AI).

Las reglas de formato se configuran por tipo de control (coordenadas, alturas, colores, tooltips) desde la configuración del addin. Claude las aplica directamente sobre el código de la ventana abierta en el editor, sin necesidad de copiar y pegar.

---

## Características

- Reformatea el bloque `WINDOW...END` del archivo activo directamente en el editor
- **Reglas por tipo de control** (WINDOW, PROMPT, ENTRY, TEXT, CHECK, LIST, BUTTON, SHEET, etc.): coordenadas, alturas, anchos, color, TIP y reglas extra
- **Dos pasadas**: Claude reformatea la ventana y luego verifica el resultado contra las reglas, corrigiendo incumplimientos
- Botón para importar el **Protocolo Clarion v2.5** como valores por defecto
- Soporte de **múltiples perfiles**: uno por proyecto, cada uno con sus propias reglas
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

| Pestaña | Contenido |
|---------|-----------|
| **Controles** | Reglas por tipo de control (por perfil). Botón para importar el Protocolo v2.5 como punto de partida |
| **Notas adicionales** | Instrucciones extra en texto libre (por perfil) |
| **API Claude** | API Key de Anthropic y modelo a usar (global, todos los perfiles) |

### Reglas por control

Para cada tipo de control se puede definir:

| Campo | Ejemplo |
|-------|---------|
| Y base / Incremento Y | `23` / `14` |
| X etiqueta / X control | `11` / — |
| Altura / Ancho mín. / Ancho máx. | `9` |
| Color | `COLOR(00E0F0FFh)` |
| Generar TIP + plantilla | `REQUERIDO - {LABEL} (Enter para validar)` |
| Reglas adicionales | Texto libre, una regla por línea |

Los campos vacíos no se envían. Con las reglas definidas, el addin arma el protocolo que recibe Claude. El documento [protocolo/Protocolo_WindowFormatter.md](protocolo/Protocolo_WindowFormatter.md) queda como referencia del Protocolo v2.5 en que se basan los valores por defecto.

---

## Uso

1. Abrir un archivo `.clw` en el editor del IDE con un bloque `WINDOW`
2. Ir a **Tools → Formatear ventana con IA (Claude)...**
3. Claude reformatea la ventana (paso 1/2), verifica el resultado contra las reglas (paso 2/2) y lo aplica directamente en el editor
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

`Services/EditorService.cs` deriva de [Clarion Assistant](https://github.com/ClarionLive/ClarionAssistant) (Copyright (c) 2025-2026 ClarionLive, MIT). Su aviso de copyright y licencia se conserva en [LICENSE](LICENSE).

---

## Contribuciones

Los PRs son bienvenidos.
