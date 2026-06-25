# Estrategia RSI Expert de Rompimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Adaptación de la estrategia "RSI_Expert" de MetaTrader 5 que opera rompimientos de umbrales del RSI.
- Usa un único indicador RSI para detectar reversiones de momentum cerca de las regiones de sobreventa/sobrecompra.
- Implementa la gestión original de take-profit fijo, stop-loss y trailing-stop expresados en pips.

## Lógica de la estrategia
1. Construir el RSI sobre la serie de velas seleccionada (periodo por defecto: 14).
2. Rastrear los dos valores de RSI completados más recientes.
3. Ir largo cuando el RSI vuelve a subir por encima del umbral inferior (20 por defecto) después de haber estado por debajo.
4. Ir corto cuando el RSI vuelve a caer por debajo del umbral superior (60 por defecto) después de haber estado por encima.
5. Cerrar cualquier exposición opuesta antes de abrir una nueva posición para mantener la dirección neta.
6. Gestionar las operaciones abiertas con distancias opcionales de stop-loss, take-profit y trailing-stop medidas en pips.

## Parámetros
| Nombre | Descripción | Por defecto |
| ---- | ----------- | ------- |
| `CandleType` | Marco temporal usado para la agregación de velas. | Velas de 1 hora |
| `TradeVolume` | Tamaño de orden usado para entradas. | 0.1 |
| `RsiPeriod` | Longitud de lookback del RSI. | 14 |
| `RsiUpperLevel` | Umbral del RSI que señala una reversión bajista. | 60 |
| `RsiLowerLevel` | Umbral del RSI que señala una reversión alcista. | 20 |
| `TakeProfitPips` | Distancia del take-profit en pips (0 deshabilita). | 60 |
| `StopLossPips` | Distancia del stop-loss en pips (0 deshabilita). | 0 |
| `TrailingStopPips` | Distancia del trailing-stop en pips (0 deshabilita el trailing). | 15 |
| `TrailingStepPips` | Mejora mínima de precio antes de que el trailing-stop se desplace de nuevo. | 5 |

> **Interpretación de pips:** En el port de StockSharp un "pip" equivale a un `Security.PriceStep`. En símbolos FX con cotización fraccionada, asegúrese de que el paso de precio coincida con la convención de pips del instrumento; de lo contrario, ajuste las distancias de entrada en consecuencia.

## Gestión de riesgos
- El take-profit y el stop-loss se evalúan en cada vela completada usando el precio promedio de posición más reciente.
- El trailing-stop se activa solo después de que el movimiento supera `TrailingStopPips + TrailingStepPips` y luego sigue el cierre por `TrailingStopPips` a medida que el precio avanza.
- Las verificaciones de stop usan los máximos/mínimos de las velas para emular disparadores intra-barra; cuando se activan, la posición se cierra a mercado.

## Notas de conversión
- Se usa la API de alto nivel (`SubscribeCandles` + `Bind`), y los valores del RSI se consumen directamente desde el callback de vinculación sin búferes de indicadores manuales.
- La lógica del trailing-stop reproduce las condiciones MQL, incluido el umbral de paso antes de cada ajuste.
- La estrategia restablece el estado de trailing cada vez que la exposición cambia o se cierra para evitar que niveles obsoletos pasen a un nuevo trade.
