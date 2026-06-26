# Estrategia de iMA iStochastic Custom
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el experto de MetaTrader **"iMA iStochastic Custom"** dentro del framework de StockSharp. Combina un envolvente de media móvil con un filtro de oscilador estocástico. El trading tiene lugar en las velas completadas del marco temporal seleccionado (`CandleType`). Todos los comentarios a continuación utilizan la misma nomenclatura que el asesor original.

Componentes clave:

1. **Envolvente de media móvil** – la media móvil base se desplaza por `LevelUpPips` y `LevelDownPips` (expresados en pips) para construir bandas de resistencia y soporte. El método de promediación coincide con las opciones de MetaTrader: Simple, Exponencial, Suavizada (SMMA) y Ponderada Lineal (LWMA).
2. **Oscilador estocástico** – las longitudes de %K, %D y suavizado siguen los parámetros originales. Dos umbrales (`StochasticLevel1` y `StochasticLevel2`) validan condiciones de sobrecompra/sobreventa.
3. **Gestión monetaria** – el selector original de `lot`/`risk` se preserva a través del parámetro `ManagementMode`. En modo `FixedLot`, el tamaño de la orden equivale a `VolumeValue`. En modo `RiskPercent`, la estrategia arriesga el porcentaje configurado del patrimonio de la cartera contra la distancia de stop-loss, reproduciendo el comportamiento de `CMoneyFixedMargin`.
4. **Protecciones** – las distancias de stop-loss, take-profit y trailing se ingresan en pips. El trailing se actualiza en velas completadas, replicando la lógica MQL mientras permanece compatible con el modelo de eventos de StockSharp.

## Lógica de trading
Las señales largas y cortas son simétricas:

- **Compra** cuando el cierre de la vela está por encima del envolvente superior (`ma + LevelUpPips`) y cualquiera de las líneas del estocástico está por encima de `StochasticLevel1`.
- **Venta** cuando el cierre de la vela está por debajo del envolvente inferior (`ma + LevelDownPips`) y cualquiera de las líneas del estocástico está por debajo de `StochasticLevel2`.
- Establecer `ReverseSignals = true` intercambia la dirección de entrada.

Solo una posición neta está activa a la vez. Cuando la señal cambia, la estrategia envía una orden suficientemente grande para aplanar la exposición actual y abrir una nueva posición en la dirección opuesta.

## Control de riesgo y salidas
- **Stop-loss / take-profit** – distancias en pips convertidas mediante el `PriceStep` del instrumento. Se verifican en cada vela finalizada usando el máximo/mínimo de la vela.
- **Trailing stop** – comienza después de que el precio se ha movido `TrailingStopPips` a favor de la posición. Requiere una mejora adicional de `TrailingStepPips` antes de cada ajuste, igual que la rutina de trailing MQL.
- **Gestión monetaria** – en modo de riesgo el tamaño de posición es `equity * VolumeValue / 100 / perUnitRisk`, donde `perUnitRisk` es la pérdida monetaria por un lote hasta el stop-loss.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal usado para los cálculos. |
| `StopLossPips`, `TakeProfitPips` | Distancias protectoras en pips. |
| `TrailingStopPips`, `TrailingStepPips` | Activación del trailing y paso (pips). Establecer cero para deshabilitar. |
| `ManagementMode`, `VolumeValue` | Dimensionamiento de lote fijo o porcentaje de riesgo. |
| `MaPeriod`, `MaShift`, `MaMethod` | Longitud de la media móvil, desplazamiento de barras y método (SMA/EMA/SMMA/LWMA). |
| `LevelUpPips`, `LevelDownPips` | Desplazamientos del envolvente superior/inferior en pips. Se permiten valores negativos para la banda inferior. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Configuración del oscilador. |
| `StochasticLevel1`, `StochasticLevel2` | Niveles de confirmación para verificaciones de compra/venta. |
| `ReverseSignals` | Invertir la dirección de todas las operaciones. |

## Notas de implementación
- Las velas, indicadores y órdenes están conectados a través de la API de alto nivel (`SubscribeCandles().BindEx(...)`).
- El tamaño del pip se ajusta automáticamente a símbolos forex de 3/5 dígitos multiplicando el `PriceStep` cuando es necesario.
- La lógica de trailing se ejecuta en velas completadas. Si se requiere trailing intrabarra, conectar la lógica a datos de nivel tick.
- No se proporciona port en Python; la carpeta `PY` está intencionalmente ausente según lo solicitado.

## Diferencias respecto a la versión de MetaTrader
- El dimensionamiento de riesgo es explícito y se basa en métricas del portafolio de StockSharp en lugar de la clase auxiliar `CMoneyFixedMargin`. Los lotes resultantes coinciden con el comportamiento original cuando el stop-loss está habilitado; con stop-loss cero el tamaño de posición permanece cero, reflejando la salvaguarda MQL.
- Las verificaciones de protección (stop-loss, take-profit, trailing) se evalúan en velas completadas porque las estrategias de StockSharp son orientadas a eventos. Esto mantiene la lógica determinista y coincide con las restricciones de backtesting.
- El logging se simplifica a la salida estándar de StockSharp; el flag verboso `InpPrintLog` no se transfiere.

Utilice esta estrategia como reemplazo directo al migrar de MetaTrader a StockSharp Designer o Runner. Ajuste los parámetros para adaptarse al instrumento y marco temporal objetivo.
