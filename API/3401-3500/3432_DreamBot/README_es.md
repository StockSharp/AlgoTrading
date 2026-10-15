# Estrategia DreamBot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
DreamBot es una versión StockSharp del asesor experto MetaTrader 4 "DreamBot". La estrategia monitorea el oscilador del Índice de Fuerza en velas horarias y espera a que el impulso cruce los umbrales alcistas o bajistas. Cuando el Índice de Fuerza cruza por encima del nivel alcista después de estar por debajo de él en la barra anterior, la estrategia abre una posición larga. Cuando el Índice de Fuerza cruza por debajo del nivel bajista después de estar por encima de él, la estrategia abre una posición corta. El comercio se produce sólo cuando no existe una posición, lo que refleja la lógica de posición única del robot original.

## Lógica comercial
- Suscríbase a velas H1 y calcule un índice de fuerza suavizado (longitud 13 por defecto).
- Realice un seguimiento de los dos últimos valores del índice de fuerza completados. Las señales se generan utilizando los valores de barra *anteriores*, exactamente igual que la implementación MT4 (`iForce` con turno 1 y 2).
- Ingrese en largo cuando el índice de fuerza de la vela anterior esté por encima de `BullsThreshold` y el valor de dos velas atrás esté por debajo del umbral, siempre que no haya ninguna posición abierta.
- Entre en corto cuando el índice de fuerza de la vela anterior esté por debajo de `BearsThreshold` y el valor dos velas atrás esté por encima del umbral, siempre que no haya ninguna posición abierta.
- El trailing stop opcional replica el EA original: una vez que las ganancias superan `TrailingStepPoints`, se retira un nivel de stop a `TrailingStartPoints` del precio y sigue nuevos avances.

## Gestión de riesgos
- `StartProtection` adjunta órdenes clásicas de stop-loss y take-profit utilizando la distancia de "puntos" de MetaTrader convertida a través del paso del precio del instrumento.
- La protección de seguimiento se basa en el mercado: cuando se supera el nivel de seguimiento calculado, la estrategia envía una orden de mercado para cerrar la posición inmediatamente.
- El seguimiento de posiciones captura el precio de entrada ponderado por volumen para que la lógica de seguimiento se alinee con los rellenos parciales y las reversiones.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `ForcePeriod` | Período de suavizado del índice de fuerza (predeterminado 13). |
| `TakeProfitPoints` | Distancia de obtención de beneficios en MetaTrader puntos. |
| `StopLossPoints` | Distancia de stop-loss en MetaTrader puntos. |
| `BullsThreshold` | Umbral del índice de fuerza alcista que permite entradas largas. |
| `BearsThreshold` | Umbral del índice de fuerza bajista que permite entradas cortas. |
| `EnableTrailing` | Habilita la lógica del trailing stop. |
| `TrailingStartPoints` | Distancia (en puntos) mantenida entre el precio y el trailing stop una vez activado. |
| `TrailingStepPoints` | Beneficio (en puntos) requerido antes de que se active el trailing stop. |
| `CandleType` | Marco de tiempo utilizado para los cálculos del índice de fuerza (por defecto, velas H1). |

## Notas
- La validación del parámetro evita que el activador de seguimiento (`TrailingStepPoints`) supere la distancia de seguimiento (`TrailingStartPoints`), coincidiendo con la verificación de seguridad MetaTrader.
- La aplicación del nivel de parada del EA original (corredor `MODE_STOPLEVEL`) se aproxima a través de las conversiones de precio-escalón de StockSharp. Dependiendo de las limitaciones del corredor, es posible que se requiera una validación adicional.
- Todos los comentarios y registros del código se proporcionan en inglés según lo solicitado por las pautas de conversión.
