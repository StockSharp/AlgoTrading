# Estrategia Swing Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia SwingTrader** es una StockSharp versión del MetaTrader 4 asesores expertos `SwingTrader.mq4`. El EA original busca
Bollinger Inversiones de banda: cuando el precio rebota desde la banda exterior y la siguiente barra cruza la línea media, el asesor abre una
posición y comienza a construir una cuadrícula de promedio estilo martingala. La estrategia traducida reproduce el mismo comportamiento de alto nivel.
usando StockSharp velas, Bollinger bandas de `StockSharp.Algo.Indicators` y los ayudantes de pedidos del marco (`BuyMarket`,
`SellMarket`). El escalado de volumen, el ancho de la cuadrícula y las reglas de liquidación reflejan el código MT4 respetando el intercambio.
límites proporcionados por los metadatos `Security`.

## Lógica comercial
1. Suscríbase al período de tiempo configurado (`CandleType`) y alimente un indicador de Bollinger Bandas con `BollingerPeriod` longitud y un
multiplicador de desviación estándar fijo de `2`.
2. Trabaje sólo con velas terminadas; la devolución de llamada del indicador ignora las barras parcialmente formadas para replicar el MT4 `IsNewCandle()`
guardia.
3. Realice un seguimiento de si la vela anterior tocó la banda superior o inferior. El par booleano `_upTouch` / `_downTouch` sigue al
Lógica de alternancia original que mantiene solo un lado activo hasta que se toca la banda opuesta.
4. Cuando no hay ninguna cesta abierta:
   - abra una posición larga si la última barra completa cruzó por encima de la banda media después de tocar previamente la banda inferior;
   - abra una posición corta si la barra cruzó por debajo de la banda media después de tocar la banda superior.
El volumen del primer pedido es igual a `InitialVolume` (después del redondeo de intercambio) y el ancho inicial de la cuadrícula es igual a la última distancia
entre las bandas superior e inferior Bollinger.
5. Cuando exista una canasta, esté atento a movimientos adversos de un ancho de banda completo desde el primer llenado:
   - para posiciones largas, si el mínimo de la vela está al menos un ancho de banda por debajo del precio del ancla, compre otra porción cuyo tamaño se multiplique
por `Multiplier` con cada nuevo nivel;
   - para cortos, si el máximo de la vela está un ancho de banda por encima del precio ancla, venda una porción adicional usando el mismo
lógica multiplicadora.
6. Continúe agregando nuevos pedidos hasta alcanzar el objetivo de ganancias o de pérdida máxima tolerada.

## Gestión del dinero y salidas.
- El asistente `CalculateUnrealizedProfit` reproduce el cálculo de PnL flotante de MT4 convirtiendo las diferencias de precios en precios.
pasos (`Security.PriceStep`) y valor del paso (`Security.StepPrice`).
- El indicador de capital invertido utiliza la fórmula original `Lots * Price / TickSize * TickValue / 30`, donde `Lots` se convierte en la suma
de los volúmenes de la cuadrícula y los parámetros de tick provienen de `Security`.
- Cierra toda la cesta una vez que el beneficio flotante supere `TakeProfitFactor * invested capital`.
- Forzar una liquidación de emergencia cuando la pérdida flotante alcance `10 * TakeProfitFactor * invested capital` (la misma proporción que la
código MT4).
- Todas las salidas se ejecutan con órdenes de mercado en dirección opuesta; Una vez plano, el estado de la cuadrícula se restablece y se deben realizar nuevos toques.
detectado antes de que se pueda activar otra entrada.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TakeProfitFactor` | `decimal` | `0.05` | Multiplicador aplicado al capital invertido para definir el objetivo de beneficio. |
| `Multiplier` | `decimal` | `1.5` | Multiplicador de volumen por cada pedido promedio adicional. |
| `BollingerPeriod` | `int` | `20` | Número de velas utilizadas por el indicador Bollinger Bandas. |
| `InitialVolume` | `decimal` | `1` | Volumen base de la primera operación en una nueva cesta (redondeado a los límites del lugar). |
| `CandleType` | `DataType` | plazo de 15 minutos | Plazo utilizado para la generación de señales. |

## Diferencias con el EA original
- StockSharp trabaja con posiciones netas; la estrategia mantiene listas explícitas de entradas de la cuadrícula para emular el orden basado en tickets de MT4
manipulación.
- En su lugar, los filtros de volumen de Exchange (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`) se aplican automáticamente
de llamar manualmente a `CheckVolumeValue`.
- Las señales se evalúan sobre velas cerradas; Los disparadores intrabar de la versión MT4 se aproximan mediante el uso de máximos y mínimos de velas.
para promediar decisiones.
- Las órdenes siempre se envían como instrucciones de mercado, mientras que MT4 usaba `OrderSend` con parámetros de oferta/demanda explícitos.

## Notas de uso
- Proporcionar metadatos realistas para el instrumento negociado: `PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep` y `MaxVolume` deben
se completará para que los cálculos de ganancias, pérdidas y volumen coincidan con el comportamiento de MT4.
- Debido a que la cuadrícula promedio escala geométricamente, pruebe la configuración con datos históricos y considere el margen del corredor.
requisitos antes de ejecutarlo en vivo.
- El ancho de la cuadrícula es igual al ancho de banda Bollinger actual; cambiar `BollingerPeriod` afecta directamente tanto el tiempo de entrada como la grilla
espaciado. Valide la sensibilidad durante la optimización.
