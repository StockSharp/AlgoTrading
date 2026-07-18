# Ruptura del rango de reserva anticipada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Early Bird Range Breakout es una adaptación de C# del asesor experto MetaTrader 4 `earlyBird1`. El sistema rastrea los máximos y mínimos de un rango previo a la comercialización configurable, aplica un filtro RSI de 14 períodos para decidir el sesgo comercial y entra en la primera ruptura una vez que se abre la sesión regular. Conserva la restricción de operación única por dirección del asesor experto original, la lógica de seguimiento controlada por la volatilidad y la disciplina de cierre diario.

## Lógica estratégica
### Construcción de rango
* **Ventana de tiempo**: el rango se calcula entre `Range Start Hour` y `Range End Hour` (después de aplicar la lógica de compensación del horario de verano). Cada vela que cruza esta ventana expande el límite alto/bajo.
* **Búfer de entrada**: se agrega un desplazamiento configurable en pips por encima del rango alto y se resta por debajo del rango bajo para imitar el búfer de ruptura `±2/Fakt` del script MetaTrader.
* **Restablecimiento diario**: el rango, los activadores de entrada y los contadores de operaciones se reinician con la primera vela terminada de cada nuevo día de negociación.

### Filtro direccional
* **RSI en aperturas**: la estrategia alimenta el RSI con precios de apertura de velas, que coinciden con la implementación MT4 que utilizó `iRSI(..., PRICE_OPEN)`.
* **Selección de polarización**: cuando RSI está por encima de 50, solo se activa el disparador largo; cuando RSI es 50 o menos, solo el disparador corto está activo. Esto garantiza una configuración unidireccional por vela, como el EA original.

### Reglas de entrada
* **Sesión de negociación**: se permiten nuevas posiciones solo en días hábiles entre `Session Start` y `Session End` después de que el rango de ruptura haya terminado de formarse.
* **Intento único por lado**: una vez que se abre una posición larga (o corta), el lado correspondiente se desactiva durante el resto del día, reflejando los contadores comerciales diarios en el código MT4.
* **Cambio de cobertura**: con `Allow Hedging` habilitado, la estrategia puede revertirse de una posición corta a una larga (o viceversa) enviando suficiente volumen para aplanar la exposición existente e inmediatamente cambiar de dirección. Cuando la cobertura está desactivada, las entradas se omiten a menos que la posición sea plana.

### reglas de salida
* **Riesgo fijo y objetivo**: los niveles de limitación de pérdidas y obtención de beneficios se expresan en pips. El objetivo de ganancias se limita automáticamente por la distancia de parada y por el ancho del rango, reproduciendo la lógica `MathMin` del asesor experto original.
* **Seguimiento impulsado por la volatilidad**: una vez que el rango de la vela actual excede el rango promedio de 16 períodos multiplicado por `Trailing Risk`, y la operación obtiene ganancias por al menos `Trailing Trigger`, el stop es seguido por la distancia completa del stop mientras la toma de ganancias se acerca (la mitad del disparador de seguimiento), coincidiendo con el comportamiento de `OrderModify` en el código MQL.
* **Cierre de sesión**: a la hora de cierre configurada, las operaciones rentables se cierran inmediatamente. Las posiciones perdedoras trasladan su obtención de beneficios al precio de entrada, al igual que la aplicación del punto de equilibrio MT4.

## Parámetros
* **Auto Trading**: interruptor maestro de habilitación para entradas automatizadas.
* **Permitir cobertura**: permite invertir en la dirección opuesta incluso cuando una posición ya está abierta.
* **Dirección comercial**: limita la estrategia a solo largo (`1`), solo corto (`2`) o ambas direcciones (`0`).
* **Volumen** – volumen de pedidos para entradas al mercado.
* **Take Profit (pips)** – distancia máxima para el objetivo de ganancias; la distancia efectiva está limitada por el stop-loss y el ancho del rango.
* **Stop Loss (pips)**: distancia de parada de protección fija en pips.
* **Disparador de seguimiento (pips)**: excursión favorable mínima requerida antes de que la lógica de seguimiento pueda ajustar el stop y la toma de ganancias.
* **Riesgo de seguimiento**: multiplicador aplicado al rango de velas promedio de 16 períodos al evaluar si la volatilidad es lo suficientemente alta como para seguir el rastro.
* **Búfer de entrada (pips)**: compensación de pips aplicada a los límites del rango al calcular los niveles de ruptura.
* **Hora/minuto de inicio de sesión**: inicio de la ventana de negociación activa (tiempo del gráfico antes del ajuste del horario de verano).
* **Hora de finalización de la sesión**: final de la ventana de negociación para nuevas posiciones.
* **Hora de cierre**: hora después de la cual las posiciones se ven obligadas a alcanzar el punto de equilibrio o cerrarse.
* **Hora de inicio del rango/Hora de finalización del rango**: horas que definen el rango previo a la sesión utilizado para los grupos de trabajo.
* **Inicio del horario de verano/Inicio del horario de invierno**: marcadores de día del año utilizados para cambiar entre compensaciones de una y dos horas, imitando la lógica `Sommerzeit/Winterzeit`.
* **RSI Longitud**: número de períodos para el filtro RSI (predeterminado 14).
* **Tipo de vela**: período de tiempo principal que impulsa los cálculos (el valor predeterminado es velas de 15 minutos).

## Notas adicionales
* El tamaño del pip se deriva del nivel de precio actual (≥ 10 unidades → `0.01`; de lo contrario, `0.0001`) exactamente igual que el cálculo `Fakt` en el script MT4.
* Las estadísticas finales utilizan las últimas 16 velas terminadas, excluyendo la barra actual, coincidiendo con la lógica de promedio original.
* La estrategia StockSharp utiliza posiciones netas, por lo que se emulan posiciones largas y cortas simultáneas mediante la sobrecompra o sobreventa de la exposición existente cuando la cobertura está habilitada.
* Sólo se proporciona la implementación de C#; Ninguna versión de Python acompaña a esta estrategia.
