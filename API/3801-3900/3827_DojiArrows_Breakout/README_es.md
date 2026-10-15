# Estrategia de flechas Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Doji Arrows** es una StockSharp adaptación del MetaTrader asesor experto `Doji_arrows_expert1.mq4`. La idea comercial es detectar una vela doji neutral e inmediatamente negociar la ruptura que sigue en la siguiente barra. Cuando el mercado imprime una vela de cuerpo muy pequeña (apertura ≈ cierre) y la vela posterior cierra más allá del máximo o mínimo del doji, la estrategia interpreta el movimiento como una ruptura direccional y entra en esa dirección.

## Lógica comercial
- **Ventana de detección de señal**: la estrategia almacena continuamente las dos velas completadas anteriormente. La vela más antigua debe ser un doji, mientras que la vela más reciente confirma la ruptura.
- **Definición de Doji**: una vela califica como doji cuando la diferencia absoluta entre apertura y cierre es menor o igual a `DojiBodyThresholdSteps * PriceStep`. Con el umbral predeterminado de 1 paso, la barra puede desviarse un tic como máximo.
- **Confirmación de ruptura** –
  - Configuración larga: la vela que sigue al doji se cierra por encima del máximo del doji más el filtro opcional `BreakoutBufferSteps`.
  - Configuración corta: la vela que sigue al doji se cierra por debajo del mínimo del doji menos el mismo buffer.
- **Señalización de un solo disparo**: la estrategia recuerda si la barra anterior ya activó una señal larga o corta y solo reacciona ante una nueva ruptura. Este comportamiento refleja el experto original que generó una flecha por secuencia de ruptura.
- **Ejecución de orden** –
  - Si aparece una ruptura contra una posición opuesta existente, la estrategia primero la cierra y luego ingresa en la nueva dirección con el volumen `Volume + |Position|` para invertir y abrir la nueva operación.
  - En estado neutral, abre una orden de mercado en la dirección de ruptura.

## Gestión de riesgos
- **Stop-loss inicial**: después de cada entrada, la estrategia coloca un nivel de protección interno `InitialStopSteps * PriceStep` lejos del precio de cumplimiento.
- **Obtención de ganancias fija**: sale de parte o de la totalidad de la posición cuando el precio alcanza `TakeProfitSteps * PriceStep` desde la entrada.
- **Trailing stop**: una vez que la operación se mueve a favor de más de `TrailingStopSteps * PriceStep`, el nivel de stop se sigue vela por vela, asegurando ganancias y permitiendo que se ejecute el movimiento.
- Todos los cálculos de protección se realizan en pasos de precios nativos, lo que hace que la lógica sea independiente del instrumento.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de vela/período de tiempo a analizar. | marco de tiempo de 5 minutos |
| `DojiBodyThresholdSteps` | Cuerpo doji máximo expresado en incrementos de precio. | 1 |
| `BreakoutBufferSteps` | Filtro adicional por encima/debajo del extremo doji antes de aceptar una fuga. | 0 |
| `InitialStopSteps` | Distancia inicial de stop-loss desde la entrada en pasos. | 20 |
| `TakeProfitSteps` | Distancia de toma de ganancias desde la entrada en pasos. | 25 |
| `TrailingStopSteps` | La distancia del trailing stop se mantiene una vez que la operación genera ganancias. | 10 |

Todos los parámetros se exponen a través de `StrategyParam<T>`, haciéndolos visibles en la interfaz de usuario y listos para la optimización.

## Notas de implementación
- La clase se basa en la suscripción de velas de alto nivel API (`SubscribeCandles().Bind(...)`) para mantenerse sincronizada con las mejores prácticas del marco.
- El estado entre llamadas se mantiene con `_previousCandle` y `_twoCandlesAgo`, lo que garantiza que solo las velas terminadas participen en la toma de decisiones.
- Los niveles de protección se almacenan por separado para posiciones largas y cortas y se restablecen cuando las posiciones se cierran o cuando los datos del mercado son insuficientes.
- Las declaraciones de registro brindan información sobre la detección de señales, los eventos de parada de pérdidas y toma de ganancias, lo que simplifica la depuración durante las pruebas retrospectivas.

## Consejos de uso
1. Valide los umbrales de tick predeterminados en cada instrumento: aumente `DojiBodyThresholdSteps` para mercados volátiles donde las impresiones exactas de doji son raras.
2. Optimice `BreakoutBufferSteps` para filtrar pequeñas rupturas falsas cuando los diferenciales o el ruido sean significativos.
3. Combine la estrategia con superposiciones de riesgo externo (parada de cartera, filtros de sesión comercial) si la implementa en varios símbolos simultáneamente.
4. Debido a que las señales dependen de velas completadas, elija un tipo de vela compatible con su horizonte comercial deseado (por ejemplo, 1 minuto para especulación, 15 minutos para entradas oscilantes).
