# Mañana Tarde Stochastic Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere el asesor experto MetaTrader 5 **Expert_AMS_ES_Stoch** (Morning/Evening Star con confirmación de Stochastic) a StockSharp. La implementación mantiene el reconocimiento de patrones de velas originales y las reglas de confirmación estocástica mientras utiliza la suscripción de velas de alto nivel API para que cada decisión se tome en barras terminadas.

## Lógica de la estrategia
- **Indicadores**
  - Oscilador Stochastic estándar con `%K`, `%D` y períodos de desaceleración configurables.
  - Promedio móvil simple del tamaño del cuerpo de la vela (`open-close` absoluto) para clasificar las velas como "largas" o "pequeñas" al igual que la versión MQL.
- **Entrada larga**
  - Patrón Morning Star en las últimas tres velas completadas:
    1. Hace dos barras: cuerpo bajista largo cuyo tamaño supera la media del cuerpo.
    2. Barra anterior: vela de cuerpo pequeño que cierra y abre por debajo de la vela anterior.
    3. Barra actual: cierre alcista por encima del punto medio de la primera vela.
  - La línea de señal Stochastic (`%D`) está por debajo del umbral de sobreventa (predeterminado `30`).
  - La exposición corta existente se aplana antes de abrir la posición larga.
- **Short Entry**
  - Patrón Evening Star que refleja las reglas anteriores.
  - Stochastic `%D` está por encima del umbral de sobrecompra (predeterminado `70`).
  - La exposición larga existente se cierra antes de abrir la operación corta.
- **Posición de salida**
  - Los cortos se cierran cuando `%D` cruza por encima del nivel de recuperación rápida (`20`) o del nivel extremo (`80`).
  - Los largos se cierran cuando `%D` cruza por debajo de `80` o `20`.
  - Estos cruces reproducen las "condiciones de cierre" del módulo de señales MQL.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco de tiempo (u otro `DataType`) utilizado para la detección de patrones y todos los indicadores. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K`, `%D` y períodos de desaceleración del oscilador estocástico. |
| `StochasticOverbought`, `StochasticOversold` | Umbrales de línea de señal utilizados para confirmar entradas de estrella vespertina/matinal. |
| `PatternAveragePeriod` | Número de velas terminadas utilizadas para promediar el tamaño del cuerpo (`|abrir-cerrar|`). |
| `ShortExitLevel`, `LongExitLevel` | `%D` niveles que fuerzan salidas cortas/largas cuando se cruzan en dirección opuesta. |

## Notas de implementación
- Las velas se procesan a través de `SubscribeCandles().BindEx(...)`; el código solo funciona con velas terminadas y nunca invoca `GetValue()` en indicadores.
- El promedio del tamaño corporal se basa en `SimpleMovingAverage` alimentado con cuerpos de velas absolutos para reproducir el ayudante `AvgBody()` de la biblioteca MQL.
- Las comprobaciones de patrones se implementan con métodos auxiliares dedicados para mantener legible la lógica de decisión y reflejar las reglas `CCandlePattern` originales.
- Antes de entrar en la dirección opuesta, la estrategia cierra cualquier exposición existente para igualar el comportamiento del Asesor Experto de operar una posición neta a la vez.

## Diferencias con el experto MQL5
- La administración de dinero, el trailing stop y la configuración de lotes fijos del marco MetaTrader no se reproducen; El volumen de pedidos StockSharp está controlado por la propiedad de la estrategia `Volume`.
- El oscilador Stochastic utiliza la implementación del indicador de StockSharp; los umbrales siguen siendo configurables para que pueda ajustar el comportamiento si el feed del agente original produjo valores ligeramente diferentes.
- El registro proporciona explicaciones detalladas (en inglés) para cada entrada y salida para ayudar en la depuración y las pruebas retrospectivas.
