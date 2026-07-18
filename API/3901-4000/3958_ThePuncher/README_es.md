# La estrategia del golpeador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Puncher es un sistema de inversión de impulso convertido del asesor experto original MetaTrader 4 "The Puncher by L. Bigger". Combina un oscilador lento Stochastic con un filtro clásico RSI para negociar condiciones extremas de sobrecompra y sobreventa. Cuando ambos osciladores coinciden en que el mercado está extendido, la estrategia busca una reversión al cierre de la vela y ingresa una orden de mercado en la dirección opuesta.

## Lógica de trading
- **Configuración de compra:** Se activa cuando la línea de señal Stochastic y RSI caen simultáneamente por debajo del nivel de sobreventa. La posición corta existente, si la hay, se cierra primero y luego se abre una nueva posición larga.
- **Configuración de venta:** Se activa cuando ambos osciladores superan el nivel de sobrecompra. Cualquier posición larga abierta se liquida antes de colocar una nueva posición corta.
- **Reglas de salida:** Las posiciones se cierran mediante señales opuestas o mediante reglas de protección (stop-loss, take-profit, breakeven y trailing stop).

La estrategia procesa solo velas terminadas del período de tiempo seleccionado para evitar el ruido dentro de la barra y replica el comportamiento de "negociar al cierre de la barra" de la fuente EA.

## Gestión del riesgo
- **Stop-loss/take-profit:** Distancias fijas opcionales medidas en pips. Cuando está deshabilitada (cero), se ignora la protección correspondiente.
- ** Punto de equilibrio: ** Mueve el stop al precio de entrada después de que la operación acumula el margen de beneficio solicitado.
- **Trailing stop:** Sigue el precio con una distancia configurable y un paso mínimo para que el stop se ajuste solo después de que el precio avance lo suficiente.
- **Volumen:** Los pedidos utilizan un parámetro de volumen fijo, que refleja la entrada de tamaño de lote de la versión MT4.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen comercial para nuevas entradas. | `1` |
| `StochasticLength` | Longitud retrospectiva del oscilador Stochastic (%K). | `100` |
| `StochasticSignalPeriod` | Período de suavizado de %K antes de aplicar la línea de señal. | `3` |
| `StochasticSmoothingPeriod` | Período de suavizado para la línea de señal %D. | `3` |
| `RsiPeriod` | Periodo de cálculo del filtro RSI. | `14` |
| `OversoldLevel` | Umbral compartido por los osciladores para detectar condiciones de sobreventa. | `30` |
| `OverboughtLevel` | Umbral compartido por los osciladores para detectar condiciones de sobrecompra. | `70` |
| `StopLossPips` | Distancia del tope de protección (0 lo desactiva). | `2000` |
| `TakeProfitPips` | Distancia del objetivo de beneficio (0 lo desactiva). | `0` |
| `TrailingStopPips` | Distancia del trailing stop (0 lo desactiva). | `0` |
| `TrailingStepPips` | Movimiento mínimo favorable antes de apretar el trailing stop. | `1` |
| `BreakEvenPips` | Beneficio necesario antes de mover el tope al punto de equilibrio. | `0` |
| `CandleType` | Tipo de datos utilizado para construir velas. | `M15` |

## Notas
- El tamaño del pip se deriva del paso del precio del valor o de los decimales, lo que garantiza que las distancias de parada y seguimiento respeten la precisión del instrumento.
- La estrategia es adecuada para pruebas retrospectivas discrecionales en las que se utilizó el EA original y puede servir como base para futuras mejoras en StockSharp.
- Las alertas de audio, los correos electrónicos y las etiquetas en los gráficos de la versión MT4 se omiten intencionalmente porque son características específicas de la plataforma.
