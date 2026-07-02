# EMA Estrategia de seguimiento cruzado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es la conversión de StockSharp del MetaTrader 4 asesor experto ubicado en `MQL/8606/EMA_CROSS_2.mq4`. Preserva la idea original de rastrear la relación entre una media móvil exponencial lenta y rápida y abrir una posición de mercado única cuando se produce un cruce. Las salidas protectoras (takeprofit, stop loss y trailing stop) se manejan a través del asistente de alto nivel `StartProtection`, por lo que el comportamiento refleja la implementación de MetaTrader mientras se utilizan las mejores prácticas de StockSharp.

## Lógica comercial
- Construya velas con el `CandleType` configurable (barras de 15 minutos por defecto) y alimente dos indicadores EMA: el EMA lenta usa `SlowEmaLength` y el EMA rápida usa `FastEmaLength`.
- Mantenga la última dirección del EMA lento en relación con el EMA rápido. La primera vela completa después de que se forman ambos indicadores se usa solo para inicializar esta dirección, al igual que la guardia `first_time` en el asesor original.
- Cuando el EMA lento se mueve por encima del EMA rápido (la nueva dirección se convierte en `1`) y la estrategia es plana, envíe una orden de compra de mercado. Cuando el EMA lento se mueve por debajo del EMA rápido (la nueva dirección se convierte en `2`) y la estrategia es plana, envíe una orden de venta de mercado. Esto reproduce exactamente el mapeo arriba/abajo de la función MQL `Crossed(LEma, SEma)`.
- Sólo puede haber una posición activa a la vez. Mientras una operación está abierta (o la orden de entrada aún está pendiente), se ignoran los cruces adicionales.

## Gestión comercial y de riesgos
- `StartProtection` configura las distancias de toma de ganancias, stop loss y trailing stop en unidades de precio calculadas a partir del instrumento `PriceStep`. Las paradas dinámicas son opcionales: establezca `TrailingStopPips` en cero para desactivarlas.
- Las órdenes se realizan con `BuyMarket`/`SellMarket` y se cierran por mercado cuando se activa cualquier nivel de protección, exactamente igual que `OrderSend` y la lógica de seguimiento del asesor original.
- El tamaño del lote base está controlado por `OrderVolume`. Antes de cada entrada se alinea con el paso de volumen del instrumento, mínimo y máximo para evitar rechazo.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia en pips (pasos de precio) utilizada para la toma de ganancias protectora. Predeterminado: 20. |
| `StopLossPips` | Distancia en pips utilizada para el stop loss de protección. Predeterminado: 30. |
| `TrailingStopPips` | Distancia de seguimiento en pips. Establezca en `0` para deshabilitar el seguimiento. Predeterminado: 50. |
| `OrderVolume` | Tamaño de lote de las entradas al mercado antes de la alineación. Predeterminado: 2. |
| `FastEmaLength` | Periodo de la EMA rápida aplicado a los precios de cierre. Predeterminado: 5. |
| `SlowEmaLength` | Período de la EMA lenta aplicada a los precios de cierre. Predeterminado: 60. |
| `CandleType` | Plazo para la construcción de velas. Predeterminado: 15 minutos. |

## Notas
- La estrategia espera hasta que ambos EMA estén completamente formados antes de reaccionar ante un cruce, eliminando la verificación `Bars < 100` del script MQL y logrando la misma estabilidad.
- Debido a que solo se utilizan órdenes de mercado, no hay llamadas `OrderModify` individuales. El módulo de protección incorporado reposiciona automáticamente el trailing stop de la misma manera que el bucle MetaTrader actualizó `OrderStopLoss`.
- No se proporciona ningún puerto Python (por solicitud); solo se incluye la implementación de C#.
