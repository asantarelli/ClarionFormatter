================================================================================
PROTOCOLO CLARION v2.5 - GUIA DE REFERENCIA COMPLETA
================================================================================
Fecha: 2025-01-10
Autor: Sistema de Desarrollo
Version: 2.5 Final (rev 3 - regla gap 4px entre botones)

================================================================================
1. SISTEMA DE COORDENADAS Y ESPACIADO
================================================================================

Posicionamiento Vertical:
* Primer PROMPT: siempre en Y = 23
* Columna X PROMPT: siempre en X = 11
* Espaciado vertical entre filas: 14 puntos  [CORREGIDO: era 13]
* Secuencia Y de PROMPTs: 23, 37, 51, 65, 79, 93, 107, 121...
* ENTRY Y = PROMPT Y + 1  [CORREGIDO: era PROMPT Y - 1]
  -> Con PROMPT H=10 y ENTRY H=9: comparten el mismo borde inferior (bottom-aligned)
  -> PROMPT: [Y .. Y+10]   ENTRY: [Y+1 .. Y+10]
  -> El PROMPT sobresale 1px arriba del ENTRY

Alineacion Horizontal -- Gap PROMPT a campo:
* X_CAMPO = X_PROMPT + W_PROMPT_mas_largo + 5
  El gap entre el borde derecho del PROMPT mas largo y el campo es de 5px.
* Todos los campos de la ventana (de una misma seccion/grupo visual) se alinean
  en la MISMA columna X, determinada por el PROMPT mas largo de esa seccion.
* VERIFICAR SIEMPRE: borde_derecho_PROMPT_mas_largo + 5 = X_campo
  Si el gap real es menor a 5px o mayor a ~10px, corregir el W del PROMPT.

EXCEPCION -- PROMPT excepcionalmente largo:
* Si hay un PROMPT mucho mas largo que los demas (ej. 'Comprobante de Debito:'),
  incluirlo en la regla pushea la columna demasiado a la derecha y desperdicia
  espacio para las filas de PROMPT corto.
* En ese caso: excluirlo de la regla de columna. Los demas ENTRYs van en la
  columna determinada por el 2do PROMPT mas largo. El PROMPT largo tiene su
  propio ENTRY a su X natural (PROMPT_autowidth + 11 + 5).

STRINGs inline (misma fila que un ENTRY):
* Y = PROMPT Y (mismo Y que el PROMPT, NO Y_ENTRY)
* H = 10 (igual que PROMPT)
* X = ENTRY_right + gap (tipicamente 5px) o F2_button_right + gap

Espaciado de Controles Compuestos:
* Boton Lookup/F2/Calendar inline: X = ENTRY_right + 5, H = 10, Y = PROMPT Y
* String de Lookup: X = BUTTON_right + gap

================================================================================
2. ALTURAS DE CONTROLES
================================================================================

* PROMPT (una linea): H = 10
* PROMPT (multi-linea / texto largo que ocupa mas de un renglon): mantener original
* ENTRY, LIST/DROP, SPIN, CHECK: H = 9
* STRING inline (misma fila): H = 10 (igual que PROMPT)
* STRING standalone: H segun contenido
* Botones Calendar / Lookup / F2 / a la derecha de un ENTRY: H = 10
* Botones principales (OK, Cancelar, Guardar, etc.): H = 14
* TEXT: mantener altura original (minimo 20px)
* GROUP: ajustar solo contenido interno

Nota de alineacion PROMPT/ENTRY:
  PROMPT H=10, ENTRY Y = PROMPT Y + 1, ENTRY H=9
  -> PROMPT: [Y .. Y+10]  (10px alto)
  -> ENTRY:  [Y+1 .. Y+10] (9px alto, mismo borde inferior)
  -> Visualmente: PROMPT y ENTRY comparten el borde inferior.
     El PROMPT sobresale 1px por arriba.

================================================================================
3. ESQUEMA DE COLORES SEMANTICO
================================================================================

Colores por funcion (usar COLOR, NO BCOLOR):
* COLOR(00E0F0FFh) - Azul claro para campos obligatorios (REQ)
* COLOR(00F0F0F0h) - Gris claro para campos readonly
* COLOR(00E8FFE8h) - Verde claro para campos monetarios ($ en picture)
* COLOR(00F8FFFFh) - Amarillo muy suave para areas TEXT
* COLOR(00F8F8F8h) - Gris muy claro para listas

Jerarquia de colores:
1. Obligatorio (maxima prioridad visual)  -> 00E0F0FFh
2. ReadOnly (informacion no editable)     -> 00F0F0F0h
3. Monetario (contexto especial)          -> 00E8FFE8h
4. Tipo de control (TEXT, LIST, etc.)

================================================================================
4. MEJORAS DE TOOLTIPS
================================================================================

Tooltips Descriptivos y Contextuales:
Agregar Tooltips a todos los controles (Excepto SHEET, PROMPT, STRING)

// Campos requeridos:
TIP('REQUERIDO - Descripcion del campo (Enter para validar)')

// Campos con lookup:
TIP('Codigo del registro - F2 para buscar, Enter para validar')

// Campos calculados:
TIP('Monto total - Se calcula: Cantidad x Precio unitario')

Patrones por tipo de control:
* Campos REQ: Iniciar con "REQUERIDO - "
* Campos lookup: Mencionar "F2 para buscar"
* Campos calculados: Explicar formula brevemente
* Campos criticos: Agregar contexto de uso

================================================================================
5. REGLAS DE LIMPIEZA DE CODIGO
================================================================================

CONSERVAR TODO (CRITICO):
* #SEQ()      - Critico para templates
* #ORIG()     - Requerido por compilador
* #ORDINAL()  - CRITICO para relaciones de lookups
* #LINK()     - Enlaces de campos
* #FIELDS()   - Metadatos de campos

MEJORAR UNICAMENTE:
* Tooltips: Mas descriptivos con contexto y shortcuts
* Mensajes: Mas informativos y utiles
* Coordenadas y alturas segun protocolo
* Colores semanticos

ELIMINAR (si existen):
* JOIN en controles SHEET - Causa problemas de renderizado en versiones modernas

================================================================================
6. DIMENSIONES DE VENTANA
================================================================================

Tamano Optimizado:
* Calcular contenido minimo necesario
* Agregar margen de seguridad: 5px en cada direccion
* Evitar dimensiones arbitrarias -- Usar solo el espacio funcional
* Proporciones balanceadas sin desperdiciar espacio

Botones inferiores:
* Posicion Y: Alto_ventana - 19px  (formula verificada)
* Alto: H = 14
* Separacion entre botones: 4px  [REGLA CONFIRMADA rev 3]
* Posicionar de DERECHA A IZQUIERDA desde el boton ancla (ultimo a la derecha):
    X_anterior = X_siguiente - W_anterior - 4
* Ejemplo validado ("Examinando Comp. de Debito Pendientes"):
    Cerrar AT(322,Y,54,14)
    Seleccionar: 322 - 62 - 4 = 256  -> AT(256,Y,62,14)
    Consulta:    256 - 54 - 4 = 198  -> AT(198,Y,54,14)
* VERIFICAR SIEMPRE: X_siguiente - (X_anterior + W_anterior) = 4 para cada par

Relacion SHEET / botones / ventana:
* SHEET_bottom = Y_botones - 3    (gap de 3px entre SHEET y botones)
* SHEET_H = SHEET_bottom - SHEET_Y = (Y_botones - 3) - 2
* H_ventana = Y_botones + 14 + 3  (boton + margen inferior)
* Verificacion: Y_botones = H_ventana - 19

Compresion de ventana en formularios cortos:
* Si el espacio entre el ultimo control y el SHEET bottom supera ~8px, comprimir.
* Formula para ajuste comprimido:
    SHEET_bottom  = ultimo_control_bottom + 9
    Y_botones     = SHEET_bottom + 3
    H_ventana     = Y_botones + 14 + 3
* Ejemplo validado ("Localidades", 2 filas):
    ultimo_control_bottom = 46 -> SHEET_bottom=55 -> Y_botones=58 -> H=75

================================================================================
7. MARGENES DE GROUP
================================================================================

Margenes estandar (multiples controles):
* Superior: 6 puntos desde borde superior del GROUP
* Izquierdo: 5 puntos desde borde izquierdo del GROUP
* Inferior: 5 puntos desde borde inferior del GROUP
* Derecho: Mantener proporcion segun contenido

Caso especial: GROUP con un unico TEXT:
* Margen Izquierdo: 9 puntos (X_TEXT = X_GROUP + 9)
* Margen Superior: 15 puntos (Y_TEXT = Y_GROUP + 15)
* Ancho TEXT: Ancho_GROUP - 18
* Alto TEXT: Alto_GROUP - 25

================================================================================
8. PROCESO DE APLICACION PASO A PASO
================================================================================

Pasos para optimizar una ventana de formulario:
1. Identificar el PROMPT mas largo de la seccion (excluyendo excepciones)
2. Calcular X_campo = 11 + W_PROMPT_mas_largo + 5
   Verificar que todos los campos arrancan en esa X
3. Aplicar Y con espaciado de 14px desde Y=23 (23, 37, 51, 65, 79, 93...)
   ENTRY Y = PROMPT Y + 1 (bottom-aligned)
4. Alturas: PROMPT H=10; ENTRY/CHECK/LIST/SPIN H=9; Lookup/Calendar/F2 H=10; Botones principales H=14
5. STRINGs inline: H=10, Y = PROMPT Y (no ENTRY Y)
6. Aplicar esquema de colores semantico con COLOR()
7. Ajustar GROUP y TEXT (regla single-TEXT si corresponde)
8. Calcular tamano de ventana: SHEET_bottom, Y_botones, H_ventana
9. Verificar gap entre botones adyacentes = 4px
   Posicionar de derecha a izquierda: X_ant = X_sig - W_ant - 4
10. Verificar alineacion visual y aplicar ajustes finos (+/-1px)

Regla de alineacion de columna -- excepcion:
* Si hay un PROMPT cuyo ancho auto excede en mucho al resto, excluirlo del
  calculo de columna comun. Su ENTRY va en su X natural (no en la columna comun).

Flexibilidad del protocolo:
* Adaptable por ventana -- No aplicar ciegamente
* Ajustes visuales permitidos -- +/-1px para perfeccion visual
* Contenido sobre formula -- Priorizar legibilidad real

================================================================================
9. EJEMPLO PRACTICO
================================================================================

ANTES:
PROMPT('Saldo:'),AT(6,52,42,10),USE(?Saldo:Prompt)
ENTRY(@N$-16.2),AT(55,52,60,9),USE(LOC:Saldo),READONLY

DESPUES (protocolo v2.5 rev 2):
PROMPT('Saldo:'),AT(11,65,,10),USE(?Saldo:Prompt)  ! Y: 51->65 (3ra fila: 23+14+14+14=65)
ENTRY(@N$-16.2),AT(70,66,60,9),USE(LOC:Saldo),COLOR(00F0F0F0h),READONLY
  ! ENTRY Y = PROMPT Y + 1 = 66
  ! X_ENTRY = 70 (determinado por el PROMPT mas largo de la seccion)
  ! COLOR(00F0F0F0h) para READONLY

================================================================================
10. OBJETIVO DEL PROTOCOLO v2.5
================================================================================

Optimizacion inteligente que balancea:
* Consistencia visual entre ventanas
* Flexibilidad para contenido especifico
* Compacidad sin sacrificar usabilidad
* Preservacion total de funcionalidad
* Mejoras graduales y sostenibles

================================================================================
11. REGLAS ESPECIFICAS PARA VENTANAS BROWSE
================================================================================

Estas reglas complementan las generales y aplican a ventanas de tipo Browse.

!!CRITICO -- REGLA UNIVERSAL QUE NO TIENE EXCEPCION!!
* El primer control visible (CHECK de filtro, PROMPT locator, etc.) SIEMPRE va
  en X=11, Y=23 -- exactamente igual que en formularios. SIN EXCEPCION.
* El COMBO/STRING locator adyacente tambien va en Y=23.
* Si no se aplica X=11 Y=23 al primer control, toda la ventana queda mal.
* STRING cuyo texto contiene 'Ctrl-B: Buscar - F3: Próximo...' se alinea a la derecha con el borde derecho del sheet que contiene al browse, y su borde superior con el del sheet.

TIPOS DE BROWSE:
* Tipo A -- CRUD dentro del TAB/SHEET; Cerrar/Seleccionar/Exportar a nivel ventana
* Tipo B -- LIST y botones todos a nivel ventana; SHEET solo como contenedor visual


--------------------------------------------------------------------------------
11.1 BOTONES A NIVEL VENTANA (Cerrar, Seleccionar, Exportar)  [BROWSE]
--------------------------------------------------------------------------------
* Y = Alto_ventana - 18px  (browse usa -18, los formularios usan -19)
* Gap entre SHEET bottom y botones ventana: 4px  (browse: 4px, formularios: 3px)
  -> SHEET_bottom = Y_botones_ventana - 4
  -> SHEET_H = SHEET_bottom - SHEET_Y = (Y_botones - 4) - 2
* Gap entre botones: 4px; posicionar de derecha a izquierda (seccion 6)

RESUMEN diferencias Browse vs Formulario:
  Browse:     H_ventana = Y_botones + 18  /  SHEET_bottom = Y_botones - 4
  Formulario: H_ventana = Y_botones + 19  /  SHEET_bottom = Y_botones - 3

--------------------------------------------------------------------------------
11.2 BOTONES CRUD DENTRO DEL TAB (Tipo A)
--------------------------------------------------------------------------------
* Y_CRUD = SHEET_bottom - 21  (= SHEET_bottom - H_boton(14) - margen_inferior(7))
* H botones CRUD: 14px (estandar -- corregir si vienen en 12px)

--------------------------------------------------------------------------------
11.3 FILA DE FILTRO / LOCATOR (primer control dentro del TAB)
--------------------------------------------------------------------------------
* Y = 23  !! IGUAL QUE FORMULARIOS -- NO cambiar este valor !!
* X controles izquierda (CHECK de filtro, etc.): X = 11
* COMBO adyacente: Y = 23, H = 10 (el COMBO mantiene H=10, no se baja a 9)
  X_COMBO = CHECK_right + 4
* PROMPT locator: alineado a la derecha segun espacio disponible
* STRING/ENTRY locator: Y = PROMPT Y + 1 (regla general -- seccion 1)
* STRING hint "Ctrl-B...": Y = 2 (cabecera visual, no se mueve)

--------------------------------------------------------------------------------
11.4 LIST PRINCIPAL
--------------------------------------------------------------------------------
* X = 11 (margen izquierdo estandar)
* Y = Y_fila_filtro + H_fila_filtro + 6  -> tipicamente 23 + 9 + 6 = 38
* W = SHEET_W - (X_LIST - SHEET_X) - margen_derecho
      margen_derecho ~= margen_izquierdo para simetria (~8px)
* H = Y_CRUD - Y_LIST - 4  (gap de 4px entre LIST bottom y botones CRUD)
  En Tipo B sin CRUD: H = Y_botones_ventana - Y_LIST - 4

--------------------------------------------------------------------------------
11.5 PROCESO BROWSE -- ORDEN DE CALCULO
--------------------------------------------------------------------------------
1. Colocar primer control filtro en X=11, Y=23 (CHECK H=9, COMBO H=10)
2. Y_LIST = 23 + H_filtro + 6  (tipicamente 38)
3. Establecer H_ventana segun contenido deseado
4. Y_botones_ventana = H_ventana - 18
5. SHEET_bottom = Y_botones - 4
6. SHEET_H = SHEET_bottom - 2
7. Y_CRUD = SHEET_bottom - 21
8. H_LIST = Y_CRUD - Y_LIST - 4
9. W_LIST = SHEET_W - (X_LIST - SHEET_X) - margen_derecho_simetrico

--------------------------------------------------------------------------------
11.6 EJEMPLO -- BROWSE TIPO A (validado: "Examinando Compras", SDGI4.app, H=262)
--------------------------------------------------------------------------------
  Window H=262
  Y_botones_ventana = 262 - 18 = 244
  SHEET_bottom      = 244 - 4  = 240  ->  SHEET AT(2,2,437,238)
  Y_CRUD            = 240 - 21 = 219
  Y_fila_filtro     = 23  (CHECK AT(11,23,91,9), COMBO AT(106,23,124,10))
  Y_LIST            = 23 + 9 + 6 = 38
  H_LIST            = 219 - 38 - 4 = 177  ->  LIST AT(11,38,420,177)
  Botones externos:   AT(*,244,*,14)

================================================================================
HISTORIAL DE REVISIONES
================================================================================

v2.5 inicial:
* Row spacing: 13px, ENTRY Y = PROMPT Y - 1

v2.5 rev 2 (correccion validada en SDGI0.app "Asignacion de Creditos - Proveedor"):
* Row spacing: 14px (secuencia 23,37,51,65,79,93...)
* ENTRY Y = PROMPT Y + 1  (bottom-aligned: PROMPT y ENTRY comparten borde inferior)
* STRINGs inline: H=10, Y = PROMPT Y
* Regla de excepcion para PROMPT excepcionalmente largo
* Nota: las ventanas procesadas antes de esta revision usaron spacing 13px y ENTRY Y-1.
  El impacto visual es minimo (1-2px de diferencia); se mantienen como estan salvo
  requerimiento expreso de uniformizacion.

v2.5 rev 3 (confirmado en SDGI0.app "Examinando Comp. de Debito Pendientes"):
* Gap entre botones adyacentes: 4px (era 2px en ventanas sin aplicar protocolo)
* Formula: posicionar de derecha a izquierda: X_ant = X_sig - W_ant - 4
* Verificacion obligatoria: X_sig - (X_ant + W_ant) = 4 para cada par
* Agregado paso 9 en proceso de aplicacion (seccion 8)

v2.5 rev 4 (validado en SDGI4.app "Examinando Compras"):
* Browse: H_ventana = Y_botones + 18  (formularios: +19)
* Browse: SHEET_bottom = Y_botones - 4  (formularios: -3)
* Browse: Y_CRUD = SHEET_bottom - 21
* Browse: Y_LIST = Y_filtro + H_filtro + 6 = 38
* COMBO en fila filtro: mantiene H=10, X = CHECK_right + 4
* Regla CRITICA reforzada: primer control browse SIEMPRE en X=11, Y=23

================================================================================
FIN DEL PROTOCOLO v2.5 rev 4
================================================================================

