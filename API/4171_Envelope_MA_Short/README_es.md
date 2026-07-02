# Estrategia corta MA envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia corta Envelope MA** es un puerto C# del asesor experto MetaTrader `EnvelopeMA.mq4` (ID 9533). Recrea la lógica de ruptura original de solo cortos en velas de 15 minutos combinando una envolvente de promedio móvil exponencial con dos EMA adicionales y un trío de filtros Parabolic SAR. La estrategia busca retrocesos del precio y el EMA rápido en la mitad inferior del sobre, luego arma una orden de venta pendiente en el límite inferior del sobre. Cuando se ejecuta la orden, gestiona la posición corta con niveles fijos de stop-loss y take-profit, así como reglas de salida basadas en indicadores.

## Indicadores y señales
- **Base del sobre:** Promedio móvil exponencial de máximos de velas (`EnvelopePeriod`, predeterminado 280). La banda inferior es el disparador de entrada y se calcula con un porcentaje de desviación (`EnvelopeDeviation`, predeterminado 0,08%).
- **EMA rápida:** Promedio móvil exponencial de los mínimos de las velas (`FastMaPeriod`, predeterminado 6) utilizado para confirmar el impulso antes de armar la entrada corta.
- **EMA lenta (desplazado):** Promedio móvil exponencial de los mínimos de las velas con un retraso de una barra (`SlowMaPeriod`, predeterminado 18). El valor retrasado refleja el parámetro de cambio `iMA` de MetaTrader y se utiliza tanto para la confirmación de entrada como para las decisiones de salida.
- **Parabolic SAR trío:** Tres Parabolic SAR instancias con diferentes factores de aceleración (0.03/0.5, 0.015/0.6 y 0.02/0.2) que deben ubicarse por encima del precio actual antes de que la estrategia permita una salida basada en indicadores.

La estrategia espera a que se completen las velas. Cuando el EMA rápido, el {PH001}} lento desplazado y el cierre de la vela permanecen entre los límites del sobre (por encima de la banda inferior y por debajo de la banda superior), envía una orden de stop de venta en la banda del sobre inferior. Las órdenes pendientes caducan después de aproximadamente cinco intervalos de velas si permanecen sin ejecutar.

## Gestión comercial
- **Niveles de protección:** Al ingresar, la estrategia coloca objetivos internos de stop-loss y take-profit derivados de las distancias de pips configuradas. Los movimientos de precios fuera del rango de la vela se aproximan utilizando los valores máximos y mínimos de cada barra terminada.
- **Salida del indicador:** Una posición corta se cierra anticipadamente cuando tanto las EMA como el cierre se sitúan por debajo del precio de entrada, los tres valores de SAR permanecen por encima del precio y el EMA rápido vuelve a cruzar por encima del EMA lento retrasado, imitando el comportamiento de MetaTrader.
- **Ajuste final:** Después de al menos cuatro barras, si el máximo de vela más alto desde la entrada se ha movido al menos tres pasos de precio por debajo del precio de entrada y el cierre se negocia por debajo de la banda inferior del sobre, el stop-loss se ajusta a esa banda inferior.

## Controles de riesgo
- **Protección de capital:** El parámetro `LiquidityThreshold` cierra cualquier posición corta abierta y cancela las paradas de venta pendientes si la relación entre el capital de la cartera y el saldo inicial cae por debajo del valor configurado (predeterminado 0,58).
- **Caducidad de la orden:** Las órdenes pendientes no completadas se cancelan automáticamente una vez que transcurre su vida útil de cinco barras para evitar señales obsoletas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de vela/plazo de tiempo procesado por la estrategia. | plazo de 15 minutos |
| `EnvelopePeriod` | EMA longitud utilizada como base del sobre. | 280 |
| `EnvelopeDeviation` | Ancho del sobre expresado en porcentaje. | 0,08 |
| `FastMaPeriod` | Periodo EMA rápida calculado sobre mínimos. | 6 |
| `SlowMaPeriod` | Periodo EMA lento (evaluado con un retraso de una barra). | 18 |
| `StopLossPips` | Distancia del stop-loss en pips desde el precio de entrada. | 25 |
| `TakeProfitPips` | Distancia de toma de ganancias en pips desde el precio de entrada. | 25 |
| `TradeVolume` | Volumen utilizado para órdenes pendientes y de mercado. | 1 |
| `LiquidityThreshold` | Relación mínima entre capital y saldo; los cortos se liquidan cuando se incumplen. | 0,58 |

## Notas de conversión
- El tamaño del lote MetaTrader basado en el saldo, el margen o los contrapips se reemplazó con un parámetro directo `TradeVolume` para ajustarse al modelo de ejecución StockSharp.
- La marca de tiempo de vencimiento de las órdenes pendientes se maneja dentro del ciclo de estrategia porque las órdenes StockSharp no exponen el mismo campo de vencimiento que MetaTrader.
- Los niveles de stop-loss y take-profit se evalúan frente a los máximos y mínimos de las velas para aproximarse a los activadores dentro de la barra, coincidiendo con el comportamiento del experto MQL que monitoreó los precios en las barras completadas.
