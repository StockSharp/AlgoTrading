# En estrategia aleatoria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto StockSharp del asesor experto MetaTrader 5 "al azar" (MQL ID 39835). El robot original demuestra cómo se comporta un proceso de decisión puramente aleatorio cuando se ve obligado a estar siempre en el mercado. Cada barra completa desencadena un lanzamiento de moneda que determina si la siguiente acción es comprar o vender. La versión StockSharp mantiene la misma idea pero la expresa con primitivas API de alto nivel (`SubscribeCandles`, `BuyMarket`, `SellMarket`) y se integra sin problemas con Designer o Runner.

La implementación evita intencionalmente la toma de ganancias, el stop loss o los trailingstops, reflejando el script de referencia MQL. Por lo tanto, sirve más como instrumento de prueba o como ejemplo pedagógico que como estrategia rentable.

## Lógica comercial
1. Suscríbete a la serie de velas configuradas (`CandleType`). El intervalo predeterminado es de 15 minutos para imitar el comportamiento del "período de tiempo actual" de MetaTrader.
2. Tan pronto como finalice una vela, verifique si se debe cerrar una operación anterior. Cuando `CloseBeforeReversal` está habilitado, la estrategia aplana la posición y espera la confirmación de que no queda exposición antes de emitir la siguiente orden.
3. Genere una dirección aleatoria utilizando un generador de números pseudoaleatorios. El parámetro opcional `RandomSeed` permite secuencias deterministas para pruebas retrospectivas reproducibles.
4. Envíe una orden de mercado utilizando el `TradeVolume` fijo. Las operaciones largas y cortas son simétricas y no hay órdenes de protección. El registro se puede habilitar a través de `LogSignals` para rastrear cada decisión aleatoria.

Debido a que cada vela desencadena solo una decisión aleatoria, la estrategia es plana o lleva una única posición en cualquier momento. Las posiciones sólo se invierten o cierran cuando aparece la siguiente barra.

## Gestión de pedidos y riesgos.
- Todas las entradas y salidas se realizan con `BuyMarket` / `SellMarket` utilizando el volumen configurado. No hay límites ni órdenes de parada.
- Si `CloseBeforeReversal` está deshabilitado, la estrategia puede mantener posiciones consecutivas: una nueva señal aleatoria puede abrir inmediatamente el lado opuesto sin cerrar explícitamente primero la operación anterior.
- No se implementa administración de dinero ni protección de cuenta. El propósito del puerto es reproducir el comportamiento del asesor experto de referencia para escenarios de pruebas educativas y de infraestructura.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de pedido base utilizado para cada entrada aleatoria. Debe seguir siendo positivo. |
| `CloseBeforeReversal` | Fuerza a la estrategia a cerrar la posición actual antes de realizar la siguiente operación aleatoria. |
| `LogSignals` | Escribe mensajes `AddInfoLog` cada vez que se genera una dirección aleatoria. |
| `CandleType` | Periodo de tiempo que produce la serie de velas que impulsa el lanzamiento aleatorio de la moneda. |
| `RandomSeed` | Valor inicial para el generador de números pseudoaleatorios. Utilice `0` para confiar en el reloj del sistema. |

## Notas de uso
- El puerto mantiene la ausencia de niveles de toma de ganancias y stop-loss como la referencia MQL. Cualquier control de riesgo debe agregarse manualmente si la estrategia se utiliza para experimentos con capital real.
- Las semillas deterministas son útiles para crear conjuntos de datos reproducibles al optimizar o comparar el comportamiento aleatorio.
- Se recomienda habilitar el registro durante las pruebas porque una estrategia puramente aleatoria ofrece poca información visual en el gráfico.
