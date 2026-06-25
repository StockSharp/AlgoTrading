# Estrategia GreenTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia GreenTrade es una conversión del asesor experto MQL5 original. Sigue tendencias de medio plazo combinando un filtro de pendiente de media móvil suavizada (SMMA) con confirmación de impulso del Índice de Fuerza Relativa (RSI). Las señales se calculan en velas completadas del marco temporal configurado, y la estrategia puede realizar pirámide hasta un número configurable de unidades de posición mientras aplica controles de riesgo fijos y un stop trailing basado en pasos.

## Lógica de trading
1. **Preparación de indicadores**
   - La SMMA se calcula en el precio mediano `((High + Low) / 2)` usando el parámetro `MaPeriod`.
   - El RSI se calcula en el precio de cierre con el retroceso `RsiPeriod`.
2. **Filtro de forma de tendencia**
   - Se inspeccionan cuatro muestras históricas de SMMA según los parámetros de desplazamiento de barra (`ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3`).
   - Una tendencia alcista requiere `SMMA(shift0) > SMMA(shift1) > SMMA(shift2) > SMMA(shift3)`.
   - Una tendencia bajista requiere `SMMA(shift0) < SMMA(shift1) < SMMA(shift2) < SMMA(shift3)`.
3. **Confirmación de impulso**
   - El RSI debe estar por encima de `RsiBuyLevel` para entradas largas y por debajo de `RsiSellLevel` para entradas cortas. El valor de RSI se toma en `ShiftBar` barras hacia atrás para reflejar la lógica MQL5 que ignora la vela en formación.
4. **Ejecución de órdenes**
   - Cuando se confirma una señal y no se supera el límite de posición, la estrategia envía una orden de mercado por `TradeVolume`.
   - Si existe una posición en la dirección opuesta, la estrategia primero la neutraliza y luego abre una nueva posición con el volumen configurado.
   - Si existe una posición en la misma dirección, el volumen de la operación se agrega a la exposición neta hasta `MaxPositions * TradeVolume`.

## Gestión de riesgos
- **Stop Loss / Take Profit inicial**: Cada nueva entrada establece objetivos de precio basados en `StopLossPips` y `TakeProfitPips`. Las distancias de pip se convierten en unidades de precio usando el `PriceStep` del instrumento. Los instrumentos con pasos fraccionarios (p. ej., símbolos Forex de cinco dígitos) reciben un factor adicional de 10 igual que el asesor original.
- **Stop Trailing**: Cuando la ganancia supera `TrailingStopPips + TrailingStepPips`, el stop se mueve para mantener una distancia de `TrailingStopPips`. Los movimientos adicionales requieren otro `TrailingStepPips` de mejora de precio, reproduciendo el comportamiento de trailing escalonado del código MQL.
- **Límite de posición**: El parámetro `MaxPositions` limita el número máximo de unidades de volumen. Las señales que excedan este límite se ignoran.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `MaPeriod` | Longitud de la media móvil suavizada aplicada al precio mediano. | 67 |
| `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` | Desplazamientos (en barras) usados para acceder a muestras históricas de SMMA para el filtro de forma de tendencia. | 1, 1, 2, 3 |
| `RsiPeriod` | Período de retroceso para el indicador RSI. | 57 |
| `RsiBuyLevel` | Umbral de RSI que confirma configuraciones alcistas. | 60 |
| `RsiSellLevel` | Umbral de RSI que confirma configuraciones bajistas. | 36 |
| `TradeVolume` | Volumen aplicado a cada entrada o adición. | 0.1 |
| `StopLossPips` | Distancia para el stop loss inicial en pips (0 lo deshabilita). | 300 |
| `TakeProfitPips` | Distancia para el take profit inicial en pips (0 lo deshabilita). | 300 |
| `TrailingStopPips` | Distancia entre el precio y el stop trailing una vez activado (0 deshabilita el trailing). | 12 |
| `TrailingStepPips` | Progreso adicional requerido antes de que el stop trailing se mueva nuevamente. | 5 |
| `MaxPositions` | Número máximo de unidades de volumen (`TradeVolume` múltiplos) que pueden estar activas. | 7 |
| `CandleType` | Serie de datos de velas usada para actualizaciones de indicadores. | Marco temporal de 1 hora |

## Notas
- Todos los cálculos se realizan solo en velas completadas; las velas sin terminar se ignoran para evitar señales ruidosas.
- El estado de posición se rastrea internamente para que las salidas por stop-loss, take-profit y trailing se manejen incluso cuando las órdenes protectoras no se colocan en el exchange.
- La conversión conserva el comportamiento original para la conversión de pip y la lógica de paso de trailing, mientras aprovecha la API de alto nivel de StockSharp para suscripciones y ejecución de órdenes.
