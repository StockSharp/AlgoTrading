# Estrategia KSRobot 1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia KSRobot 1.5** es una conversión de C# del MetaTrader 4 asesor experto `KSRobot_1_5_h1_v1.mq4`. La versión StockSharp mantiene la idea original de negociar las rupturas de precios de Kijun-sen confirmadas por un promedio móvil ponderado lineal (LWMA) de 20 períodos al tiempo que aplica una ventana de negociación estricta y controles de riesgo en capas. Todos los cálculos se realizan en velas de 30 minutos de forma predeterminada, pero el período de tiempo se puede cambiar mediante un parámetro.

## Datos e indicadores del mercado.
- **Ichimoku** indicador con períodos Tenkan/Kijun/Senkou Span B 12/06/24 de forma predeterminada.
- **Promedio móvil ponderado lineal (LWMA)** con longitud 20 para medir la pendiente y filtro de distancia mínima.
- **Velas con marco de tiempo** definidas por `CandleType` (por defecto, M30) para la generación de señales.

## Lógica comercial
### Flujo de trabajo largo
1. Una vela debe interactuar con la línea Kijun desde abajo. Cualquiera de las siguientes opciones es suficiente: la vela se abre por debajo y cierra por arriba, el cierre anterior fue por debajo mientras que el nuevo cierre está por arriba, o el mínimo de la vela perfora el nivel.
2. El último valor de Kijun es estable o superior a dos barras hacia atrás, lo que impide operaciones contra un movimiento bajista inmediato de la línea base.
3. El LWMA está al menos `MaFilterPips` (convertido en unidades de precio) por debajo de Kijun. Esto reproduce el requisito de que la media móvil se sitúe unos pocos pips por debajo de la línea base.
4. La pendiente LWMA es positiva (LWMA actual mayor que la barra anterior).
5. La configuración se almacena como pendiente hasta que se cumpla la condición de pendiente; solo un lado puede estar pendiente en un momento dado, imitando las banderas `longcross`/`shortcross` de MQL.
6. Cuando todos los criterios coinciden y no existe una exposición larga neta, se envía una orden de compra de mercado. El precio de entrada almacenado en caché por la estrategia se convierte en la base para la gestión de paradas, equilibrio y seguimiento.

### Flujo de trabajo corto
Se aplican condiciones de espejo:
1. La vela interactúa con Kijun desde arriba (abre arriba y cierra abajo, cierre anterior arriba y cierre actual abajo, o el máximo toca el nivel).
2. Kijun es plano o más bajo que dos barras hacia atrás.
3. La LWMA se encuentra `MaFilterPips` por encima de Kijun.
4. La pendiente LWMA es negativa en comparación con la barra anterior.
5. Solo se rastrea un corto pendiente y se borra una vez que aparece una señal larga, al igual que el experto original.
6. Cuando está satisfecho y la cuenta aún no está corta, se envía una orden de venta de mercado.

## Reglas de salida y control de riesgos.
- **Ventana de tiempo**: las nuevas operaciones solo se consideran mientras el tiempo de apertura de la vela esté dentro de `[TradingStartHour, TradingEndHour)`, hora de intercambio predeterminada de 07:00 a 19:00.
- **Stop-loss inicial**: establezca `StopLossPips` por debajo/por encima del precio de entrada (convertido mediante el tamaño del pip del instrumento). Si es cero, no se realiza un seguimiento de ninguna parada inicial.
- **Movimiento de punto de equilibrio**: tan pronto como el beneficio no realizado supera `BreakEvenPips`, el stop se mueve al precio de entrada más un pip para las posiciones largas (menos uno para las posiciones cortas). Este comportamiento está controlado por `_breakEvenStep` para emular la lógica de "mover a BE+1" de MT4.
- **Parada dinámica**: una vez que el precio avanza `TrailingStopPips`, la parada sigue esa distancia solo en la dirección favorable.
- **Take-profit**: distancia objetivo fija opcional definida por `TakeProfitPips`. Establezca en cero para desactivar.
- **Salida de pendiente**: si la LWMA se vuelve contra la operación antes de que el stop haya cruzado la entrada, la posición se cierra inmediatamente. Esto captura la salida "MA resultó incorrecta" del script MQL.
- **Prioridad**: cuando tanto el stop-loss como la toma de ganancias se tocan dentro de la misma vela, el stop-loss tiene prioridad para permanecer conservador con los datos de la vela.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Longitud tenkan-sen del indicador Ichimoku. |
| `KijunPeriod` | 12 | Longitud de Kijun-sen (disparador principal). |
| `SenkouSpanBPeriod` | 24 | Longitud Senkou Span B. |
| `LwmaPeriod` | 20 | Periodo de confirmación LWMA. |
| `MaFilterPips` | 6 | Distancia mínima de pips entre LWMA y Kijun. |
| `StopLossPips` | 50 | Distancia de parada protectora inicial. |
| `BreakEvenPips` | 9 | Beneficio requerido antes de mover el tope al punto de equilibrio. |
| `TrailingStopPips` | 10 | Distancia del trailing stop después de que el precio se convierte en beneficio. |
| `TakeProfitPips` | 120 | Distancia de toma de ganancias fija opcional. |
| `TradingStartHour` | 7 | Hora inclusiva para comenzar a procesar nuevas operaciones. |
| `TradingEndHour` | 19 | Hora exclusiva para frenar nuevas entradas. |
| `CandleType` | plazo de 30 minutos | Tipo de datos utilizado para la suscripción de velas. |

Todos los parámetros basados en pips se convierten en unidades de precio usando `Security.PriceStep` (o `MinPriceStep`). Los instrumentos cotizados con tres o cinco dígitos decimales reciben un multiplicador automático de ×10 para recrear el tamaño de pip de FX estándar.

## Notas de implementación
- La estrategia vincula los indicadores Ichimoku y LWMA a través de `SubscribeCandles().BindEx(...)`, lo que garantiza que los valores provengan directamente del canal de indicadores sin recopilaciones manuales.
- La gestión de posiciones refleja al experto en MT4: los niveles pendientes reemplazan las banderas `longcross`/`shortcross` y se borran una vez que se activa una operación.
- Los niveles de protección se almacenan en caché después de la entrada para que las decisiones de equilibrio y seguimiento funcionen con datos a nivel de vela incluso sin actualizaciones de órdenes individuales.
- `StartProtection` se invoca con distancias cero porque todas las acciones protectoras se manejan dentro del código de estrategia, coincidiendo con la lógica MT4 personalizada.
- Sólo se utilizan órdenes de mercado. La selección original de límite versus mercado se basó en ticks de oferta/demanda que no están disponibles en las pruebas retrospectivas basadas en velas.

## Uso
1. Cree la instancia de estrategia, asigne `Security`, `Portfolio`, `Volume` e iníciela dentro del entorno StockSharp.
2. Opcionalmente, ajuste los parámetros basados en pips para el instrumento específico. Los ajustes preestablecidos optimizados de los comentarios MQL (GBPUSD, EURUSD) se pueden reproducir cambiando los valores predeterminados antes de ejecutarlos.
3. Esté atento a la salida del registro: las entradas, los movimientos de equilibrio, los ajustes finales y las salidas de emergencia se informan a través de llamadas `LogInfo`.
4. Adjunte el área del gráfico generado (velas, Ichimoku, LWMA, operaciones propias) en el diseñador o backtester para visualizar el flujo comercial.

Sólo se proporciona la versión C#. No se crea ninguna carpeta de Python según los requisitos.
