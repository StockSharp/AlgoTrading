# Estrategia Breakdown Catcher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Breakdown Catcher es un sistema de ruptura barra a barra adaptado del asesor experto de MetaTrader "Breakdown catcher". Después de cada vela completada, la estrategia coloca niveles virtuales de ruptura por encima del máximo anterior y por debajo del mínimo anterior (opcionalmente desplazados por una sangría). Cuando la siguiente vela perfora uno de estos niveles, la estrategia entra en una posición en la dirección de la ruptura y asigna inmediatamente stop-loss, take-profit y protección de trailing opcional expresados en pips.

## Lógica de operación
1. Al cierre de cada vela, el máximo y el mínimo de la barra completada se convierten en el rango de referencia para el siguiente período.
2. Nivel de ruptura de compra = máximo anterior + sangría (en pips). Nivel de ruptura de venta = mínimo anterior − sangría.
3. Si la vela actual supera el nivel de compra mientras no hay posición abierta, la estrategia abre una posición larga a mercado, elimina cualquier contexto corto y almacena los niveles protectores.
4. Si la vela actual supera el nivel de venta mientras está plana, la estrategia abre una posición corta a mercado.
5. Las distancias de stop-loss y take-profit se convierten de pips a precios absolutos usando el paso de precio del instrumento y el ajuste clásico de MetaTrader para instrumentos de 3/5 decimales.
6. Un trailing stop puede ajustar el precio protector después de que el trade se mueva a favor en al menos `TrailingStop + TrailingStep` pips. El paso de trailing imita la lógica de MetaTrader donde el stop solo se mueve después de un movimiento adicional suficiente.
7. Si ambos niveles de ruptura se alcanzan dentro de la misma vela, la estrategia omite el trading para esa barra para evitar un orden de ejecución ambiguo.
8. Un filtro de spread bloquea nuevas entradas cuando el spread bid-ask actual supera los `AllowedSpreadPoints` configurados.

## Gestión monetaria
* La estrategia usa el `Strategy.Volume` base para el tamaño de la orden. Al revertir posiciones, el volumen se incrementa por el valor absoluto de la posición actual para garantizar un giro completo.
* El stop-loss, take-profit y los trailing stops se gestionan internamente emitiendo órdenes de salida a mercado cuando los rangos de precio incluyen los niveles protectores.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `StopLossPips` | Distancia de stop-loss en pips. | `30` |
| `TakeProfitPips` | Distancia de take-profit en pips. | `90` |
| `TrailingStopPips` | Distancia de trailing stop en pips. Establecer en `0` para desactivar el trailing. | `30` |
| `TrailingStepPips` | Progreso adicional requerido antes de que el trailing stop se mueva. Debe ser positivo cuando el trailing está habilitado. | `5` |
| `IndentPips` | Desplazamiento extra aplicado a los niveles de ruptura. | `0` |
| `AllowedSpreadPoints` | Spread máximo medido en puntos brutos (unidades `PriceStep`). | `5` |
| `CandleType` | Serie de velas usada para la detección de rupturas. | `marco temporal de 1h` |

## Notas y limitaciones
* La conversión de pips sigue el mismo ajuste de dígitos que el EA original: si el instrumento tiene 3 o 5 decimales, un pip equivale a diez pasos de precio.
* Dado que la API de alto nivel de StockSharp trabaja con eventos de velas, el orden exacto en que ambos niveles de ruptura son alcanzados dentro de una sola vela no puede determinarse; por ello, la estrategia omite dichas barras.
* Las órdenes protectoras se modelan con salidas a mercado, lo que garantiza que la estrategia sea autónoma sin depender de órdenes de stop del bróker.
