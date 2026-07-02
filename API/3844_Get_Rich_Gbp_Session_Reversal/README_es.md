# Hágase rico Estrategia de reversión de sesión en GBP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia Get Rich or Die Trying GBP** es un sistema de reversión a la media de alta frecuencia que transfirió el asesor experto MetaTrader 4 "Get Rich or Die Trying GBP" al API de alto nivel de StockSharp. La lógica monitorea un breve historial de velas diminutas y abre operaciones cerca de dos momentos predefinidos del día cuando las velas más recientes se han cerrado en su mayoría en contra de la dirección esperada. Este enfoque intenta capturar un rápido retroceso inmediatamente después de que se superpongan las sesiones de Londres y Nueva York.

## Lógica de trading
1. La estrategia se suscribe a velas de 1 minuto de forma predeterminada (el tipo de vela se puede personalizar).
2. Se mantiene una ventana móvil de las últimas velas terminadas *Lookback*. Cada vela se clasifica como:
   - `+1` si cerró por debajo de su apertura (vela bajista).
   - `-1` si cerró por encima de su apertura (vela alcista).
   - `0` si la vela es neutral.
3. La suma acumulada de estas clasificaciones se utiliza para decidir la dirección del comercio:
   - Una suma positiva significa que dominan las velas bajistas y la estrategia se prepara para una entrada **larga**.
   - Una suma negativa significa que dominan las velas alcistas y la estrategia se prepara para una entrada **corta**.
4. Los pedidos solo se pueden realizar durante los primeros *EntryWindowMinutes* minutos después de la hora en la que la hora actual del servidor coincide con una de las dos horas objetivo:
   - `FirstEntryHour + HourShift` (predeterminado: medianoche de Londres después de la corrección GMT+2).
   - `SecondEntryHour + HourShift` (predeterminado: 21:00 hora del servidor para la superposición cercana de Nueva York).
5. Si no hay ninguna posición abierta y se cumplen todas las condiciones, la estrategia envía una orden de mercado con el tamaño de lote fijo o el tamaño dinámico calculado a partir del bloque de administración de dinero.
6. Mientras estamos en una posición, la estrategia aplica tres reglas de salida independientes:
   - Una **toma de ganancias parcial** cierra la operación una vez que el precio de cierre se mueve a favor de *PartialTakeProfitPoints*.
   - Un **stop-loss duro** se activa cuando el precio se mueve *StopLossPoints* pasos de precio contra la operación.
   - Un **trailing stop** bloquea las ganancias después de que el mercado supera los niveles de precios de *TrailingStopPoints*, utilizando el máximo más alto (para posiciones largas) o el mínimo más bajo (para posiciones cortas) visto desde la entrada.
7. También se controla como red de seguridad un nivel final de obtención de beneficios igual a los pasos de precio de *TakeProfitPoints*.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TakeProfitPoints` | 100 | Distancia máxima de beneficio (en pasos de precio) monitoreada según la lógica de seguimiento. |
| `PartialTakeProfitPoints` | 40 | Distancia de obtención de beneficios principal (en incrementos de precio) que replica la salida anticipada del EA original. |
| `StopLossPoints` | 100 | Distancia de stop-loss (en pasos de precio). |
| `TrailingStopPoints` | 30 | Distancia del trailing stop (en incrementos de precio). |
| `FixedVolume` | 1 | Volumen de orden base en lotes cuando la administración del dinero está deshabilitada. |
| `UseMoneyManagement` | falso | Permite dimensionar la posición dinámica según el valor de la cuenta y el riesgo configurado. |
| `RiskPercent` | 10 | Porcentaje del valor de la cartera respecto del riesgo por operación cuando la gestión del dinero está activa. |
| `Lookback` | 18 | Número de velas terminadas utilizadas en el recuento alcista/bajista. |
| `FirstEntryHour` | 22 | Primera hora de negociación antes de la corrección del turno de hora. |
| `SecondEntryHour` | 19 | Segunda hora de negociación antes de la corrección del turno de hora. |
| `HourShift` | 2 | Corrección de zona horaria aplicada a ambos horarios de negociación. |
| `EntryWindowMinutes` | 5 | Ancho de la ventana de entrada (minutos desde el inicio de la hora de clasificación). |
| `CandleType` | plazo de 1 minuto | Tipo de vela a la que suscribirse; Se puede sustituir por cualquier otro tipo de vela periódica. |

## Gestión monetaria
Cuando `UseMoneyManagement` está habilitado, la estrategia estima el volumen de la orden arriesgando `RiskPercent` del valor actual de la cartera sobre el `StopLossPoints` configurado. El cálculo respeta el paso del lote del instrumento y el volumen mínimo para seguir cumpliendo con el intercambio.

## Notas de uso
- Las ventanas de negociación se evalúan utilizando la hora de intercambio/servidor de las velas entrantes. Ajuste `HourShift` para que `FirstEntryHour + HourShift` y `SecondEntryHour + HourShift` coincidan con los límites de sesión deseados.
- `Lookback` debe permanecer mayor que 1 para evitar decisiones ruidosas. Aumentarlo suaviza la medición del sentimiento a costa de reacciones más lentas.
- La lógica protectora se basa en velas prefabricadas. Si se requiere precisión intrabar, reduzca la duración de la vela en consecuencia.
- El experto original MQL permitía múltiples posiciones simultáneas; este puerto limita la exposición a una única posición abierta para igualar las mejores prácticas de StockSharp.

## Limitaciones
- El trailing stop es virtual y se ejecuta enviando una salida de mercado en la siguiente vela terminada después de que el precio cruza el umbral de seguimiento.
- El dimensionamiento de administración de dinero supone que `Security.StepPrice` representa correctamente el valor monetario de un paso de precio. Valide este mapeo para cada instrumento antes de operar en vivo.

## Requisitos
- StockSharp entorno API de alto nivel (solución AlgoTrading).
- Velas minuto históricas y en tiempo real para el instrumento GBP negociado.

## Referencias
- Original MetaTrader 4 asesor experto: `MQL/7690/Get_rich_or_die_trying_any_gbp.mq4`.
