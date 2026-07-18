# Estrategia universal MA Cross V4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Universal MA Cross V4** es una versión StockSharp de alto nivel del asesor experto MetaTrader 4 "Universal MACross EA v4". El algoritmo sigue la interacción entre una media móvil rápida configurable y una media móvil lenta. Admite varios tipos de medias móviles, fuentes de precios seleccionables, una ventana de negociación horaria y una gestión de posiciones flexible que incluye comportamiento de parada y reversión, objetivos de protección y paradas dinámicas. La estrategia está diseñada para la ejecución basada en barras utilizando API de alto nivel de StockSharp con suscripciones de velas.

## Lógica de trading
### Procesamiento de indicadores
* Se evalúan dos medias móviles en cada vela terminada. Cada media móvil puede utilizar su propia longitud, método de suavizado (simple, exponencial, suavizado o ponderado lineal) y fuente de precio (cierre, apertura, máximo, mínimo, mediana, típico o ponderado).
* El filtro **MinCrossDistancePoints** requiere que los promedios rápido y lento diverjan al menos en el número especificado de pasos de precio en la barra de cruce. Cuando **ConfirmedOnEntry** está habilitado, la divergencia se valida en la vela completada anteriormente, reproduciendo el modo "confirmado" del EA original.
* La configuración de **ReverseCondition** intercambia señales alcistas y bajistas sin cambiar la configuración del indicador.

### Reglas de entrada
1. Se produce una entrada larga cuando el promedio rápido cruza por encima del promedio lento en al menos **MinCrossDistancePoints**. Una entrada corta requiere el cruce opuesto.
2. Cuando **StopAndReverse** es verdadero, una señal opuesta cierra la posición activa antes de que se consideren nuevas entradas.
3. **OneEntryPerBar** evita múltiples entradas dentro de la misma vela al rastrear la marca de tiempo del pedido más reciente.
4. El tamaño del pedido está controlado por **TradeVolume**. StockSharp aplica automáticamente este volumen a las órdenes de mercado generadas.

### Gestión de posiciones
* Las distancias de stop-loss y take-profit se definen en puntos a través de **StopLossPoints** y **TakeProfitPoints**. Se convierten a precios absolutos utilizando el paso del precio del instrumento. Cuando **PureSar** está activo, toda la lógica de protección está desactivada, al igual que la opción "Pure SAR" en la versión MQL.
* La gestión del trailing stop refleja la implementación de MQL: una vez que el precio se mueve más allá de **TrailingStopPoints** desde el nivel de entrada, el stop se sitúa detrás del mercado a la misma distancia. Las paradas dinámicas se ignoran cuando **PureSar** está habilitado.
* Los niveles de protección se controlan en cada vela cerrada. Si el rango de velas viola el stop activo o el objetivo, la estrategia cierra la posición por orden de mercado para mantener un comportamiento determinista en los datos históricos.

### Filtro de sesión
* La marca **UseHourTrade** restringe el comercio a la ventana inclusiva entre **StartHour** y **EndHour** (0–23). Los límites de la sesión finalizan alrededor de la medianoche cuando la hora de finalización es menor que la hora de inicio. La gestión de posiciones, incluidos los trailingstops, permanece activa fuera de la sesión, pero no se permiten nuevas entradas.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Longitudes de las medias móviles rápida y lenta. |
| `FastMaType`, `SlowMaType` | Métodos de media móvil: Simple, Exponencial, Suavizado o Lineal Ponderado. |
| `FastPriceType`, `SlowPriceType` | Las fuentes de precios alimentaron cada media móvil. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias de protección en los escalones de precios. Establezca en `0` para desactivar. |
| `TrailingStopPoints` | Distancia del trailing stop en pasos de precio. Establezca en `0` para deshabilitar el seguimiento. |
| `MinCrossDistancePoints` | Separación mínima entre las medias requeridas para validar un cruce. |
| `ReverseCondition` | Intercambie reglas alcistas y bajistas sin cambiar los indicadores. |
| `ConfirmedOnEntry` | Validar señales en la barra cerrada anterior. Desactívelo para confirmación inmediata. |
| `OneEntryPerBar` | Permita como máximo una nueva posición por vela. |
| `StopAndReverse` | Cierre e invierta la posición actual cuando aparezca la señal opuesta. |
| `PureSar` | Deshabilite la lógica de stop-loss, take-profit y trailing stop. |
| `UseHourTrade`, `StartHour`, `EndHour` | Filtro de sesión que restringe las entradas a un rango de horas específico. |
| `TradeVolume` | Volumen de pedidos utilizado por `BuyMarket` y `SellMarket`. |
| `CandleType` | Serie de velas suscritas para cálculos de indicadores. |

## Notas de conversión
* Las distancias basadas en precios se expresan en MetaTrader puntos. El asistente `GetPriceOffset` convierte esos valores en precios StockSharp utilizando el paso del precio del valor o la precisión decimal. Esto mantiene el comportamiento de la estrategia alineado con el EA original independientemente del instrumento.
* Los trailingstops se gestionan internamente porque StockSharp estrategias de alto nivel operan en velas terminadas. Este enfoque determinista garantiza que las pruebas retrospectivas que utilizan velas reproduzcan la lógica de seguimiento de MT4 prevista.
* No se incluye ningún puerto de Python que coincida con la solicitud de conversión. En este paquete solo se proporciona la implementación de C# y la documentación multilingüe.
