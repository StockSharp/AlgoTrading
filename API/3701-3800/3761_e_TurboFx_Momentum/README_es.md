# Estrategia de impulso e-TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **e-TurboFx Momentum Strategy** es una adaptación directa del MetaTrader 4 asesor experto original "e-TurboFx". El sistema escanea las velas terminadas más recientes y busca tramos direccionales donde los cuerpos de las velas continúan expandiéndose. Las velas bajistas consecutivas con un tamaño de cuerpo creciente indican una capitulación potencial que puede desvanecerse con una entrada larga, mientras que las velas alcistas consecutivas con cuerpos en expansión insinúan un repunte demasiado extendido que puede venderse en corto. La implementación StockSharp mantiene la lógica impulsada por eventos a través de suscripciones de velas y adjunta automáticamente protección opcional de stop-loss y take-profit.

## Lógica comercial
1. Suscríbase a un tipo de vela configurable (período de tiempo) y procese solo velas terminadas.
2. Realice un seguimiento de dos secuencias separadas: una para velas bajistas y otra para velas alcistas.
3. Para cada vela, mida el tamaño absoluto del cuerpo (`|Close - Open|`).
4. Restablezca la secuencia de dirección opuesta tan pronto como una vela se cierre en la otra dirección.
5. Dentro de cada secuencia se requieren cuerpos estrictamente en expansión: cada nueva vela debe tener un cuerpo más grande que la anterior. Cualquier contracción reinicia el contador de secuencia desde 1.
6. Cuando el número de velas en una secuencia alcanza `DepthAnalysis`, activa una entrada al mercado en la dirección opuesta a la última secuencia (comprar después de rachas bajistas, vender después de rachas alcistas).
7. Una vez que una posición esté abierta, pausar la detección de señales hasta que la estrategia regrese a una posición plana. El `StartProtection` integrado gestiona distancias opcionales de stop-loss y take-profit expresadas en pasos de precio (ticks).

Este comportamiento reproduce el algoritmo MQL4 en el que el asesor experto comprobó las últimas `N` velas cerradas y confirmó que todos los cuerpos estaban alineados en la misma dirección y que cada cuerpo era más grande que el cuerpo de la siguiente vela más antigua.

## Detalles de implementación
- Utiliza la suscripción de vela de alto nivel API con `SubscribeCandles` y `Bind` para cumplir con las pautas del proyecto.
- Mantiene solo campos escalares (`_bearishSequence`, `_bullishSequence`, `_previousBearishBody`, `_previousBullishBody`) para evitar colecciones personalizadas y depender del estado interno entre eventos.
- Llama a `StartProtection` solo una vez en `OnStarted` para configurar órdenes opcionales de stop-loss y take-profit en pasos de precio. Un valor de `0` desactiva cada orden de protección al igual que el experto original.
- Proporciona extensos comentarios en inglés en el código fuente, incluidas explicaciones para restablecimientos y activadores de entrada.
- Dibuja velas y operaciones propias en un área del gráfico cuando se ejecuta dentro de Designer o la interfaz de usuario para facilitar la depuración.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `DepthAnalysis` | Número de velas terminadas consecutivas requeridas en una dirección con cuerpos en expansión antes de abrir una operación. | `3` |
| `TakeProfitSteps` | Distancia de obtención de beneficios medida en pasos del precio de cambio (ticks). Establezca en `0` para deshabilitar la obtención de ganancias. | `120` |
| `StopLossSteps` | Distancia de stop-loss medida en pasos del precio de cambio (ticks). Establezca en `0` para desactivar el stop loss. | `70` |
| `TradeVolume` | Volumen enviado con cada orden de mercado. Cambiar este parámetro también actualiza la base `Strategy.Volume`. | `0.1` |
| `CandleType` | Tipo de datos de vela (período de tiempo) suscrito para el análisis. | `1 hour` |

Todos los parámetros numéricos exponen metadatos de optimización para que la estrategia se pueda ajustar en optimizadores StockSharp si se desea.

## Notas y recomendaciones
- Debido a que la estrategia reacciona a la expansión del cuerpo de la vela, el período de tiempo elegido afecta significativamente la frecuencia de la señal. Intervalos más cortos producen más intercambios, pero pueden requerir distancias de protección más estrechas.
- Asegúrese de que la seguridad conectada defina un `PriceStep` válido; de lo contrario, las distancias de protección escalonadas no se pueden convertir a precios absolutos.
- Realice una prueba retrospectiva del puerto dentro del diseñador StockSharp antes de la implementación en vivo para validar cómo se traducen la parada y el destino para el instrumento seleccionado.
- La estrategia mantiene una única posición abierta a la vez. Después de cada salida, los contadores se reinician y el patrón debe reconstruirse desde cero, reflejando el comportamiento original de MQL4.
