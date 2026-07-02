# Estrategia Momo Trades V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Momo Trades V3 es una estrategia de impulso convertida del asesor experto MetaTrader original. Combina un detector de patrones MACD de múltiples condiciones con un filtro de media móvil exponencial desplazada (EMA). El puerto StockSharp mantiene los elementos discrecionales del EA, agrega un manejo de equilibrio opcional y proporciona un modo de dimensionamiento de posiciones basado en el riesgo que refleja la lógica de lote automática del script.

## Lógica de trading
1. **MACD patrones de impulso** – la estrategia observa la línea principal MACD usando los parámetros clásicos `(12, 26, 9)` y un desplazamiento adicional (`MacdShift`). Se aceptan dos patrones alcistas:
   - Una secuencia estrictamente ascendente donde el tercer valor es igual a cero y las dos muestras siguientes continúan aumentando.
   - Una secuencia en la que MACD cruza por encima de cero, donde las siguientes muestras permanecen positivas mientras que los valores anteriores son negativos.
Las entradas bajistas requieren condiciones reflejadas con valores decrecientes y la línea cruzando por debajo de cero.
2. **EMA filtro de distancia**: el precio de cierre de la barra desplazada (`MaShift`) debe estar al menos `PriceShiftPoints` MetaTrader puntos por encima de EMA para operaciones largas y por debajo de EMA para operaciones cortas. Esto evita entradas cuando el precio se acerca al promedio.
3. **Régimen de posición única**: la estrategia abre una nueva posición solo cuando es plana. Las señales opuestas se ignoran mientras una operación está activa.
4. **Salida de cierre de sesión**: cuando `CloseEndDay` está habilitado, la estrategia liquida cualquier posición a las 23:00 hora de la plataforma (21:00 los viernes) para evitar la exposición nocturna.
5. **Parada de equilibrio opcional**: cuando `UseBreakeven` está activado, una vez que el precio se mueve lo suficiente como para colocar una parada en el precio de entrada más `BreakevenOffsetPoints`, la estrategia establece un nivel de equilibrio. Si el precio vuelve a ese nivel o lo supera, la posición se cierra en el mercado.

## Gestión del riesgo
- **Protección inicial**: `StopLossPoints` y `TakeProfitPoints` se convierten en distancias de precios absolutas a través del paso de precio del instrumento y se pasan a `StartProtection`, por lo que las órdenes de protección se adjuntan automáticamente.
- **Volumen automático**: si `UseAutoVolume` es verdadero, el tamaño del pedido se calcula a partir del capital de la cartera actual. La estrategia asigna `RiskFraction` de capital a la operación, lo divide por el valor del contrato (`price × lot size`), normaliza el resultado al paso del volumen de intercambio y respeta los límites `VolumeMin`/`VolumeMax`. Cuando el tamaño automático está deshabilitado, `TradeVolume` se usa directamente.

## Indicadores
- **Divergencia de convergencia de media móvil (MACD)**: entrega la señal de impulso principal y se evalúa en muestras históricas utilizando `MacdShift`.
- **Promedio móvil exponencial (EMA)**: se utiliza como filtro de tendencia desplazada.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
|------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeFrame(15m)` | Periodo de tiempo principal utilizado para la generación de señales. |
| `MaPeriod` | `int` | `22` | EMA periodo para el filtro de desplazamiento. |
| `MaShift` | `int` | `1` | Número de barras completadas utilizadas al muestrear el precio de cierre y EMA. |
| `FastPeriod` | `int` | `12` | Longitud rápida de EMA para MACD. |
| `SlowPeriod` | `int` | `26` | Longitud lenta de EMA para MACD. |
| `SignalPeriod` | `int` | `9` | Longitud de la señal EMA para MACD. |
| `MacdShift` | `int` | `1` | Se aplicó desplazamiento adicional al evaluar los patrones MACD. |
| `PriceShiftPoints` | `decimal` | `10` | Distancia mínima (en MetaTrader puntos) entre el cierre desplazado y el EMA requerido para abrir una posición. |
| `TradeVolume` | `decimal` | `0.1` | Volumen de operaciones predeterminado cuando el tamaño automático está deshabilitado. |
| `RiskFraction` | `decimal` | `0.1` | Fracción del capital de la cartera utilizada para dimensionar la orden cuando `UseAutoVolume` es verdadero. |
| `UseAutoVolume` | `bool` | `false` | Permite dimensionar el volumen basado en el riesgo. |
| `StopLossPoints` | `decimal` | `100` | Distancia inicial de stop-loss expresada en MetaTrader puntos. `0` desactiva la parada de protección. |
| `TakeProfitPoints` | `decimal` | `0` | Distancia inicial de obtención de beneficios en MetaTrader puntos. `0` desactiva el objetivo. |
| `CloseEndDay` | `bool` | `true` | Cierra posiciones abiertas cerca del final del día de negociación (23:00 o 21:00 los viernes). |
| `UseBreakeven` | `bool` | `false` | Activa la lógica de gestión del equilibrio. |
| `BreakevenOffsetPoints` | `decimal` | `0` | Compensación agregada al precio de entrada al armar el punto de equilibrio de salida. |

## Notas de uso
- Asegúrese de que el instrumento tenga un `PriceStep` válido; de lo contrario, la estrategia vuelve a un valor de `0.0001` puntos al convertir MetaTrader puntos en distancias de precios.
- El filtro MACD se basa en velas terminadas; la estrategia sale temprano para que las barras sin terminar coincidan con el comportamiento original de EA.
- Debido a que solo se permite una posición a la vez, el riesgo por operación permanece controlado por un único `TradeVolume` (o su equivalente de tamaño automático).
