# Estrategia de Cruce de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto "MA Cross" de MetaTrader 5 (archivo `MA Cross.mq5`) dentro del marco de trabajo StockSharp. El sistema observa dos medias móviles configurables y emite órdenes de mercado siempre que la media rápida cruce la media lenta. La implementación mantiene el nivel original de flexibilidad al exponer el método de media móvil, el precio aplicado y el desplazamiento del indicador para ambas curvas.

## Lógica de la estrategia
1. Suscribirse a un flujo único de velas definido por el parámetro `CandleType`.
2. Calcular las medias móviles rápida y lenta en cada vela completada. Cada media móvil puede usar uno de cuatro métodos (simple, exponencial, suavizada o ponderada linealmente) y lee uno de los precios aplicados al estilo MetaTrader (cierre, apertura, máximo, mínimo, mediana, típico o ponderado).
3. Almacenar los valores más recientes del indicador teniendo en cuenta el desplazamiento configurado, de modo que las pruebas de cruce se realicen sobre valores de barras anteriores cuando sea necesario.
4. Detectar un cruce alcista cuando la media rápida se mueve de por debajo de la media lenta desplazada a por encima. Detectar un cruce bajista cuando ocurre el movimiento contrario.
5. Emitir órdenes de mercado solo después de que ambos indicadores estén completamente formados y la estrategia esté en línea. Las señales largas cierran cualquier posición corta existente y abren una posición larga de `OrderVolume`. Las señales cortas cierran cualquier posición larga existente y abren una posición corta del mismo tamaño.

La estrategia opera estrictamente sobre velas completadas y nunca inspecciona datos sin terminar. La lógica de protección se activa a través de `StartProtection()` para garantizar que StockSharp monitoree la posición abierta en busca de condiciones anormales.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `FastPeriod` | 3 | Período de la media móvil rápida. |
| `SlowPeriod` | 13 | Período de la media móvil lenta. |
| `FastMethod` | Simple | Método de media móvil para la línea rápida (simple, exponencial, suavizada o ponderada linealmente). |
| `SlowMethod` | LinearWeighted | Método de media móvil para la línea lenta. |
| `FastPriceType` | Close | Precio aplicado usado por la línea rápida (cierre, apertura, máximo, mínimo, mediana, típico, ponderado). |
| `SlowPriceType` | Median | Precio aplicado usado por la línea lenta. |
| `FastShift` | 0 | Número de barras completadas usadas para desplazar la media rápida a la izquierda. |
| `SlowShift` | 0 | Número de barras completadas usadas para desplazar la media lenta a la izquierda. |
| `OrderVolume` | 1 | Volumen para cada orden de mercado. |
| `CandleType` | Marco temporal de 1 minuto | Serie de datos de velas procesada por la estrategia. |

Todos los parámetros pueden optimizarse dentro de StockSharp porque el constructor los registra usando los helpers `StrategyParam`.

## Reglas de trading
- **Entrada larga:** Se activa cuando la media rápida cruza por encima de la media lenta según los valores ajustados por desplazamiento. Si la estrategia ya está corta, envía una única orden de compra a mercado para cerrar la exposición corta y abrir una nueva posición larga. Si no hay posición, compra exactamente `OrderVolume`.
- **Entrada corta:** Se activa cuando la media rápida cruza por debajo de la media lenta. La exposición larga existente se invierte mediante una única orden de venta a mercado; de lo contrario, la estrategia abre una nueva operación corta de `OrderVolume`.
- **Sin escalado adicional:** Una vez posicionado, las señales en la misma dirección se ignoran hasta que ocurra el cruce opuesto.
- **Estilo de ejecución:** Las órdenes se envían con `BuyMarket` o `SellMarket`. La estrategia no configura niveles de stop-loss o take-profit; la gestión de riesgos puede añadirse a través de otros módulos de StockSharp si es necesario.

## Notas de conversión
- La creación del indicador refleja las llamadas `iMA` de MetaTrader. La enumeración personalizada `MovingAverageMethods` mapea `MODE_SMA`, `MODE_EMA`, `MODE_SMMA` y `MODE_LWMA` a `SimpleMovingAverage`, `ExponentialMovingAverage`, `SmoothedMovingAverage` y `WeightedMovingAverage` de StockSharp respectivamente.
- El manejo del precio aplicado reproduce las opciones `ENUM_APPLIED_PRICE` de MetaTrader calculando los precios mediana, típico y ponderado directamente a partir de los datos de las velas.
- Los parámetros de desplazamiento reutilizan la lógica original: la estrategia almacena en búfer los valores del indicador y recupera las comparaciones de entrada y salida de barras anteriores cuando `FastShift` o `SlowShift` son positivos.
- La lógica de gestión de posiciones coincide con el enfoque original donde las señales opuestas primero cierran la posición existente y luego establecen una posición en la nueva dirección en la misma barra.
