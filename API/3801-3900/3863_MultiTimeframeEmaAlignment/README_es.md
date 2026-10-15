# Estrategia MultiTimeframeEmaAlignmentStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**MultiTimeframeEmaAlignmentStrategy** es un puerto StockSharp del MetaTrader 4 asesores expertos `1h-4h-1d.mq4` de la carpeta `MQL/7713`. El robot original alinea promedios móviles exponenciales rápidos y lentos en tres períodos de tiempo y aplica una gestión protectora del dinero a través de niveles fijos de stop loss, takeprofit y trailing stop. Esta versión de C# sigue la misma idea de alto nivel al tiempo que aprovecha los enlaces de indicadores y los ayudantes de pedidos de alto nivel de StockSharp.

## Lógica de trading
- La estrategia se suscribe a tres series de velas simultáneamente: M1 (marco temporal de señal), M5 (filtro a medio plazo) y M30 (confirmación de tendencia de marco temporal superior).
- Cada serie alimenta un par de promedios móviles exponenciales (EMA) con longitudes configurables (por defecto 8 y 64).
- Una **configuración alcista** requiere que el EMA rápido se mantenga por encima del EMA lento en los tres marcos temporales. Además, el EMA rápida no debe perder impulso (valor actual mayor o igual al valor anterior y también por encima del valor de hace `ShiftDepth` barras).
- Una **configuración bajista** requiere que el EMA rápido se mantenga por debajo del EMA lento en los tres períodos de tiempo, con el EMA rápido disminuyendo en impulso.
- Las órdenes se activan al cierre de la vela M1 cuando se cumplen las comprobaciones de alineación e impulso. Las señales largas solo se permiten cuando no hay una posición larga abierta (o una posición corta existente se cierra primero) y viceversa.

Esta interpretación recrea la intención de las condiciones MT4 con el nivel alto API de StockSharp. Las comparaciones de MQL "cambio de MA" se emula a través del búfer `ShiftDepth` que rastrea los valores de EMA unas cuantas velas atrás y garantiza que el impulso sea consistente con la dirección de entrada.

## Gestión del riesgo
- El tamaño de la posición está controlado por el parámetro `TradeVolume` (3 lotes predeterminados como el EA original).
- Las distancias opcionales de parada de pérdidas y toma de ganancias se proporcionan en pips. Se convierten a precios a través del `PriceStep` del instrumento (vuelve a `0.0001` cuando falta).
- El trailing stop replica el comportamiento del EA al acercar el precio stop al mercado cada vez que la operación avanza lo suficiente.
- Los parámetros de riesgo se pueden alternar de forma independiente, coincidiendo con los indicadores `StopLossMode`, `TakeProfitMode` y `TrailingStopMode` del script MQL.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeVolume` | Volumen de pedidos utilizado por `BuyMarket` / `SellMarket`. Refleja la entrada `Lots`. | `3` |
| `FastLength` | EMA período para la línea rápida. | `8` |
| `SlowLength` | EMA período para la línea lenta. | `64` |
| `ShiftDepth` | Número de velas históricas utilizadas para emular las comparaciones de cambios de media móvil MQL. | `3` |
| `UseStopLoss` | Habilita stop loss fijo. | `true` |
| `StopLossPips` | Distancia de stop loss expresada en pips. | `75` |
| `UseTakeProfit` | Permite tomar ganancias. | `true` |
| `TakeProfitPips` | Distancia de toma de ganancias expresada en pips. | `150` |
| `UseTrailingStop` | Permite la gestión de trailing stop. | `true` |
| `TrailingStopPips` | Distancia de seguimiento en pips. | `30` |
| `M1CandleType` | Tipo de vela para el período de tiempo de la señal (predeterminado 1 minuto). | `1m` |
| `M5CandleType` | Tipo de vela para el filtro intermedio (por defecto 5 minutos). | `5m` |
| `M30CandleType` | Tipo de vela para el período de tiempo más alto (predeterminado 30 minutos). | `30m` |

## Notas de uso
1. Adjunte la estrategia a un instrumento y asegúrese de que los datos históricos estén disponibles para los tres períodos de tiempo para permitir que se completen los buffers EMA.
2. El parámetro `ShiftDepth` debe permanecer al menos `2` para que la estrategia pueda validar el impulso a corto plazo.
3. Cuando `UseTrailingStop` está activo sin `UseStopLoss`, la lógica de seguimiento aún inicializa un valor de parada una vez que la operación se mueve a favor.
4. Debido a que StockSharp se ejecuta al cierre de la vela, los resultados pueden diferir ligeramente de la ejecución tick a tick de la versión MT4, especialmente en mercados volátiles. El comportamiento central de alineación de tendencias permanece intacto.

## Notas de conversión
- Los cálculos de los indicadores se basan exclusivamente en el mecanismo `Bind` de StockSharp; no se utilizan recopilaciones manuales del historial de indicadores.
- La gestión de pedidos se implementa con ayudantes de alto nivel (`BuyMarket`, `SellMarket`) y seguimiento de precios interno en lugar de llamadas directas `OrderSend`.
- Las notificaciones de correo y los controles de deslizamiento del script MQL se omiten porque están fuera del alcance de StockSharp.

## Archivos
- `CS/MultiTimeframeEmaAlignmentStrategy.cs` – implementación de la estrategia principal de C#.
- `README_ru.md` – Documentación rusa.
- `README_zh.md` – Documentación china.
