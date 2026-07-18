# Estrategia STO M5xM15xM30
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión fiel de C# del MetaTrader 4 asesor experto "STO_m5xm15xm30". Utiliza tres osciladores estocásticos calculados en los marcos temporales M5, M15 y M30 para identificar cambios de impulso sincronizados. La implementación StockSharp mantiene la estructura de entrada/salida original, reemplaza la gestión manual de pedidos con el nivel alto API y expone cada clave constante como un `StrategyParam` configurable.

## Lógica de trading
1. **Confirmación de múltiples períodos**
   - El estocástico primario (M5 predeterminado) debe mostrar un cruce alcista (`%K` cruza por encima de `%D`).
   - Los valores estocásticos medio (M15 predeterminado) y lento (M30 predeterminado) ya deben ser alcistas (`%K` por encima de `%D`).
   - Una configuración bajista requiere condiciones reflejadas (`%K` debajo de `%D`).
2. **Filtro de cambio**
   - El estocástico primario también verifica el estado `ShiftBars` velas antes. Una señal de compra requiere que el `%K` histórico esté por debajo de `%D`, lo que garantiza un nuevo cruce. Las señales de venta requieren lo contrario.
3. **Filtro de impulso de precios**
   - El último cierre debe ser mayor (para compras) o menor (para ventas) que el cierre de la vela anterior. Esto refleja la regla `Close[0] > Close[1]` del script MT4.
4. **Reglas de entrada**
   - Si no hay ninguna posición abierta y se cumplen los criterios alcistas, la estrategia abre una orden de mercado larga con el `TradeVolume` configurado.
   - Si existe una posición corta cuando llega una señal alcista, primero se aplana y luego se abre una posición larga. Lo contrario ocurre con las señales bajistas.
5. **Reglas de salida**
   - Un estocástico M5 dedicado con período `ExitKPeriod` verifica la vela anterior (`shift = 1`). Una posición larga se cierra cuando `%K` cae por debajo de `%D`; un corto se cierra cuando `%K` sube por encima de `%D`.
   - Después de que se activa una salida, la estrategia omite el reingreso inmediato en la misma vela, replicando el comportamiento del bucle de órdenes MT4.

## Indicadores y Suscripciones de Datos
- Velas primarias: período de tiempo predeterminado de 5 minutos (`CandleType`).
- Velas de confirmación intermedia: período de tiempo predeterminado de 15 minutos (`MiddleCandleType`).
- Velas de confirmación lentas: período de tiempo predeterminado de 30 minutos (`SlowCandleType`).
- Osciladores Stochastic: todos usan suavizado %K = 3 y suavizado %D = 3, coincidiendo con los parámetros originales.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 5 minutos | Horario de trabajo para entradas y salidas. |
| `MiddleCandleType` | velas de 15 minutos | Plazo de confirmación #1. |
| `SlowCandleType` | velas de 30 minutos | Plazo de confirmación #2. |
| `FastKPeriod` | 5 | Período %K para el estocástico primario. |
| `MiddleKPeriod` | 5 | Período %K para el estocástico medio. |
| `SlowKPeriod` | 5 | Período %K para el estocástico lento. |
| `ExitKPeriod` | 5 | Periodo %K para el estocástico de salida que opera en la barra anterior. |
| `ShiftBars` | 3 | Número de barras entre el cruce de referencia y la barra actual. |
| `TakeProfitPoints` | 30 | Distancia protectora de toma de ganancias (puntos). |
| `StopLossPoints` | 10 | Distancia de protección stop-loss (puntos). |
| `TradeVolume` | 0.1 | Volumen de pedidos utilizado para nuevas entradas. |

Todos los parámetros se exponen a través de `StrategyParam<T>`, lo que los hace disponibles para su optimización dentro de StockSharp Designer.

## Gestión del riesgo
`StartProtection()` traduce las entradas MT4 `TP` y `SL` en StockSharp órdenes de protección. Ambos se pueden desactivar poniendo el parámetro correspondiente a cero.

## Notas de implementación
- Los valores de los indicadores se obtienen exclusivamente a través de `SubscribeCandles(...).BindEx(...)`, cumpliendo con las directrices de alto nivel API y evitando la recopilación manual de indicadores.
- El asistente `StochasticShiftBuffer` imita el argumento MT4 `shift` sin llamar a `GetValue`, manteniendo solo el historial de barras necesario.
- El procesamiento de entrada ocurre una vez por vela completada. La evaluación de salida ocurre antes de la lógica de entrada, coincidiendo con el orden de procesamiento del EA original.
- Los comentarios en línea explican cada paso del procesamiento y aclaran cómo la lógica MQL se asigna al código StockSharp.

## Uso
1. Agregue la estrategia a un esquema StockSharp o proyecto de diseñador.
2. Configure el símbolo deseado y asegúrese de que los datos históricos de las velas M5, M15 y M30 estén disponibles.
3. Ajuste los parámetros para adaptarlos al mercado objetivo o al escenario de optimización.
4. Iniciar la estrategia; Los niveles protectores de stop-loss/take-profit se registran automáticamente para cada posición.
