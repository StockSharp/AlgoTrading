# Stop Loss dinámico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El asesor experto original MetaTrader "Dynamic Stop Loss" no abre nuevas operaciones por sí solo. En lugar de eso, observa las posiciones existentes en el mercado y, una vez que aparece una nueva vela, reposiciona el stop-loss protector para que permanezca a una distancia fija detrás del último precio. El puerto StockSharp mantiene el mismo comportamiento: cada barra completa activa un nuevo cálculo de la parada de protección para cualquier lado que esté actualmente abierto. Si no existe ninguna posición, la estrategia simplemente permanece inactiva hasta que se detecta una nueva posición.

## como funciona
1. La estrategia se suscribe a velas definidas por el parámetro `Candle Type` (período de tiempo predeterminado de 1 minuto).
2. Cuando se cierra una vela, el precio de cierre se multiplica por la distancia del punto seleccionado por el usuario. La distancia se convierte de puntos de estilo MetaTrader en un delta de precio absoluto a través de `Security.PriceStep` (retroceso a `Security.Step`, luego a `1`).
3. Si se abre una posición larga, la estrategia cancela cualquier orden stop existente y coloca una nueva parada de venta en `Close - Distance`.
4. Si se abre una posición corta, el stop se mueve a `Close + Distance` mediante una orden stop de compra.
5. Cuando se cierra la posición (manualmente o mediante parada de llenado), la orden de seguimiento se cancela para evitar órdenes de protección obsoletas.

Esto produce la misma distancia de parada constantemente reanclada que la versión MQL, lo que significa que la parada puede acercarse o alejarse del mercado a medida que las velas fluctúan.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StopLossPoints` | `800` | Distancia entre el precio de mercado y el stop de protección medida en puntos del instrumento. El valor se multiplica por `Security.PriceStep` (recurre a `Security.Step`, luego `1`) antes de aplicarse al precio de cierre. Establezca en `0` para deshabilitar la gestión de paradas. |
| `CandleType` | `TimeFrameCandle(00:01:00)` | Tipo de vela que define cuándo se recalcula el stop. Elija un período de tiempo que coincida con el gráfico utilizado en MetaTrader. |

## Notas de uso
- La estrategia espera que las operaciones se abran mediante estrategias externas, operaciones manuales u otros componentes. Sólo gestiona el stop-loss.
- Asegúrese de que los metadatos de seguridad (`PriceStep`, `Step`, volumen) estén completos para que la conversión de punto a precio coincida con el tamaño de tick del corredor. Los instrumentos cotizados con pips fraccionarios deben exponer el paso adecuado.
- Debido a que el stop se recalcula en cada cierre de vela, seguirá el precio incluso cuando el mercado se mueva en contra de la posición. Esto refleja la lógica MetaTrader donde `OrderModify` siempre usa la última `Bid`/`Ask` menos/más la distancia configurada.
- Las órdenes de parada creadas siempre reemplazan a la anterior para mantener la plataforma sincronizada con el último nivel de protección.
