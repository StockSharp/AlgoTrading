# Estrategia de plantilla de red neuronal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el comportamiento de la plantilla de asesor experto MQL5 que introduce funciones RSI y MACD en una red neuronal. Debido a que StockSharp no se envía con el cargador de red personalizado del proyecto original, la estrategia reemplaza la red de caja negra con un modelo de puntuación determinista manteniendo la misma estructura de mercado y controles de riesgo. El objetivo es capturar el impulso cuando tanto RSI como MACD acuerdan la dirección y el movimiento proyectado es lo suficientemente grande como para justificar una operación.

## Indicadores y datos
- **Índice de fuerza relativa (RSI, 12 períodos)** calculado al cierre de la vela, reflejando el precio típico original.
- **Promedio móvil de convergencia y divergencia (MACD 48/12/12)** utilizado como histograma de impulso y proxy de confianza.
- **Plazo** configurable; El valor predeterminado son velas de 5 minutos para que coincida con la fuente experta.

## Lógica comercial
1. En cada vela terminada, la estrategia actualiza las colas sucesivas de valores de histograma RSI y MACD con la ventana controlada por `BarsToPattern`.
2. La desviación RSI de 50 y la desviación del histograma MACD de su media móvil se combinan en una puntuación de confianza utilizando una tangente hiperbólica para emular la función de aplastamiento de la red.
3. Si la confianza absoluta supera `TradeLevel` y el movimiento proyectado convertido en puntos supera `MinTargetPoints`, la estrategia emite una orden de mercado en la dirección sugerida por la puntuación.
4. Se almacena una toma de ganancias dinámica igual al movimiento proyectado multiplicado por `ProfitMultiply` y limitado por `MaxTakeProfitPoints` para el manejo de salida manual. Un stop-loss simétrico en puntos refleja el comportamiento original.
5. Mientras una posición está abierta, la estrategia verifica cada vela terminada: si el precio alcanza el stop u objetivo almacenado, cierra la posición en el mercado y restablece el estado interno.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `BarsToPattern` | Número de velas almacenadas en la ventana móvil utilizada para calcular las estadísticas RSI y MACD. |
| `TradeLevel` | Confianza mínima (0-1) requerida para abrir una posición. |
| `ProfitMultiply` | Multiplicador aplicado al movimiento proyectado antes de limitarlo con `MaxTakeProfitPoints`. |
| `MinTargetPoints` | Número mínimo de puntos de precio requeridos de la proyección para ingresar a una operación. |
| `MaxTakeProfitPoints` | Distancia máxima, en puntos, permitida para la toma de ganancias. |
| `StopLossPoints` | Distancia, en puntos, del stop de protección respecto al precio de entrada. |
| `TradeVolume` | Volumen enviado con cada orden de mercado. |
| `CandleType` | Tipo de datos de vela o período de tiempo al que suscribirse. |

## Notas
- El modelo de confianza es intencionalmente determinista para mantener el comportamiento transparente y al mismo tiempo preservar la estructura del enfoque de red neuronal original.
- Los niveles de toma de ganancias y límite de pérdidas se administran manualmente para que cada operación mantenga sus propios objetivos dinámicos, de manera similar a cómo la versión MQL5 utiliza la salida de la red.
- La estrategia solo evalúa nuevas entradas cuando no hay ninguna posición abierta, replicando la restricción de posición única del asesor experto fuente.
