# Estrategia de prueba Rsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
`RsiTestStrategy` convierte el asesor experto MetaTrader 4 **RSI_Test** en el nivel alto de StockSharp API. La estrategia combina un rápido filtro de impulso RSI con una simple confirmación de velas y un dimensionamiento de posiciones consciente del riesgo. Opera con un único instrumento definido por la estrategia del anfitrión y utiliza únicamente velas completadas, reflejando la lógica de tick-to-close del código original.

## Reglas de trading
1. Calcule el índice de fuerza relativa con el configurable `RsiPeriod`.
2. Vaya en largo cuando el RSI esté subiendo desde una región de sobreventa (`BuyLevel`) *y* la vela actual se abra por encima de la anterior.
3. Vaya en corto cuando el RSI esté cayendo desde una región de sobrecompra (`SellLevel`) *y* la vela actual se abra por debajo de la anterior.
4. Respete el límite `MaxOpenPositions`. Un valor de `0` desactiva el límite; de lo contrario, la exposición neta no puede exceder `MaxOpenPositions * Volume`.
5. Administre las salidas a través de un trailing stop estilo escalera que se activa una vez que el precio avanza `TrailingDistanceSteps` ticks más allá del precio de entrada promedio.
6. No se utiliza ninguna toma de ganancias explícita. Las posiciones salen cuando se activa el trailing stop o cuando la sesión de negociación finaliza la estrategia.

## Tamaño de la posición y riesgo
* La estrategia deriva un tamaño de pedido tentativo a partir de `RiskPercentage` del valor actual de la cartera. Cuando el instrumento proporciona datos de margen (`Security.MarginBuy`/`Security.MarginSell`) se respeta el capital requerido por lote; de lo contrario, el importe se divide por el último precio de cierre como alternativa conservadora.
* Los volúmenes se redondean a `Security.VolumeStep` (o dos decimales si se desconoce el paso) y se sujetan dentro del rango `Security.MinVolume`/`Security.MaxVolume`.
* Establezca `RiskPercentage` en cero para deshabilitar el tamaño dinámico y siempre intercambie el `Volume`.

## Comportamiento de parada dinámica
* `TrailingDistanceSteps` expresa la distancia en pasos de precio (`Security.PriceStep`). Si el instrumento no expone un paso, la distancia se trata como una compensación de precio directa.
* Una vez que el máximo de cierre o intrabar cruza el nivel de activación (`entry + distance` para largos, `entry - distance` para cortos), la estrategia arma el trailing stop en el mismo desplazamiento más allá del precio de entrada.
* El tope protector se aplica solo una vez por posición, exactamente igual que el EA original que mueve el tope desde el punto de equilibrio hasta el primer escalón y lo mantiene allí.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `RsiPeriod` | RSI período retrospectivo. | `14` |
| `BuyLevel` | Umbral de sobreventa que prepara una configuración larga. | `12` |
| `SellLevel` | Umbral de sobrecompra que prepara una configuración corta. | `88` |
| `RiskPercentage` | Cuota de cartera utilizada para dimensionar las posiciones. Establezca `0` para ignorar. | `10` |
| `TrailingDistanceSteps` | Distancia (en pasos de precio) requerida para armar el trailing stop. | `50` |
| `MaxOpenPositions` | Posiciones simultáneas máximas; `0` elimina el límite. | `1` |
| `CandleType` | Plazo principal para los cálculos. | `15` minutos |
| `Volume` | Volumen base cuando no se pueda resolver el dimensionamiento del riesgo. | `1` |

## Notas de uso
1. Adjunte la estrategia a un valor que exponga metadatos precisos de `PriceStep`, `VolumeStep` y margen para obtener la mejor coincidencia con el comportamiento de MQL.
2. El algoritmo verifica solo las velas completadas (`CandleStates.Finished`), por lo que las pruebas retrospectivas deben usar el mismo período de tiempo que la producción.
3. `StartProtection()` de la clase base está habilitado en `OnStarted`, lo que permite que el control de riesgos integrado de StockSharp gestione remanentes de posiciones inesperados.
4. Debido a que el asesor experto original lanzó MetaTrader optimizaciones a través de `GlobalVariableGet`, ese comportamiento se omite intencionalmente. Configure los parámetros directamente dentro de StockSharp.
5. Combine la estrategia con una cartera que actualice `Portfolio.CurrentValue` para dimensionar el riesgo dinámico. Sin él, la estrategia vuelve elegantemente al estático `Volume`.
