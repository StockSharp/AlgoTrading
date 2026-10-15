# Estrategia SurefireThing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia SurefireThing es una StockSharp versión de alto nivel del MetaTrader 4 asesor experto *Surefirething*. Opera con velas completadas, calcula los niveles de órdenes pendientes del rango de la sesión anterior y restablece la exposición al final de cada día de negociación. La lógica se centra en desplegar un par simétrico de órdenes límite que intentan capturar la reversión a la media alrededor del cierre anterior.

## Lógica de trading
- Al cierre de cada día de negociación, la estrategia intenta aplanar la posición y cancela cualquier orden pendiente activa.
- Usando la última vela completada del día anterior, mide el rango de precios `(High - Low)` y lo multiplica por `RangeMultiplier` (el valor predeterminado es 1,1 como en el EA original).
- La mitad del rango ajustado se suma al cierre anterior para obtener el precio de entrada límite de venta. La misma distancia se resta del cierre para colocar la orden de límite de compra.
- Las compensaciones de stop-loss y take-profit se expresan en incrementos de precios. Cuando el instrumento expone un `Security.Step` válido, se convierten a distancias absolutas y se administran a través de `StartProtection` para que las posiciones ocupadas reciban órdenes de protección automáticamente.
- Las órdenes se envían una vez por día de negociación. Si se llena, las manijas de protección adjuntas salen; de lo contrario, los pedidos permanecen activos hasta el próximo reinicio diario.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen presentado con cada orden pendiente. | `0.1` |
| `TakeProfitPoints` | Distancia (en pasos de precio) al objetivo de beneficio. Se convierte en un desplazamiento absoluto cuando se conoce el paso. | `10` |
| `StopLossPoints` | Distancia (en pasos de precio) hasta el tope de protección. Convertido de la misma manera que el objetivo de ganancias. | `15` |
| `RangeMultiplier` | Factor aplicado al rango de velas anterior antes de calcular los precios de entrada. | `1.1` |
| `CandleType` | Plazo primario procesado por la estrategia. El valor predeterminado es velas de 1 minuto, pero se puede ajustar para que coincida con el gráfico original. | `TimeSpan.FromMinutes(1)` |

## Notas de implementación
- API de alto nivel: las velas se consumen hasta `SubscribeCandles(CandleType)` y se procesan en el controlador `ProcessCandle` una vez terminadas.
- Restablecimiento diario: `CloseForNewDay` cancela las órdenes pendientes y cierra posiciones cada vez que se detecta un nuevo día calendario a partir de las marcas de tiempo de las velas.
- Lógica de protección: `ConfigureProtection` traduce los controles de riesgo basados en puntos en `Unit` instancias y activa `StartProtection` para que las órdenes de stop-loss y take-profit se vuelvan a crear automáticamente después de ejecutarse.
- Ciclo de vida del pedido: las referencias a los pedidos pendientes se almacenan y borran a través de `CancelPendingOrder`, así como también de `OnOrderChanged` cuando los pedidos finalizan o se cancelan.
- Normalización de precios: `Security.ShrinkPrice` se utiliza para redondear los precios calculados al tamaño del tick del instrumento antes de enviar nuevos pedidos.

## Recomendaciones de uso
- Alinee `CandleType` con el marco temporal utilizado por el EA original (normalmente el gráfico donde se adjuntó) para mantener las mismas velas de referencia.
- Ajuste `RangeMultiplier` cuando los instrumentos muestren diferentes características de volatilidad para que las órdenes pendientes permanezcan dentro de distancias realistas.
- Si el corredor impone distancias mínimas de parada, asegúrese de que `TakeProfitPoints` y `StopLossPoints` respeten esas restricciones después de la conversión a precios absolutos.
- La estrategia asume datos intradiarios continuos. Cuando se producen grandes brechas (fines de semana, días festivos), la siguiente vela disponible aún activa un reinicio y la colocación de una nueva orden basada en la última barra observada.
