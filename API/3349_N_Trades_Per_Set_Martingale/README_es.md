# N operaciones por conjunto Martingale Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión directa del asesor experto MetaTrader "N operaciones por conjunto de martingala + Cerrar y restablecer al aumentar el capital". Mantiene la dirección del mercado simple (solo se realizan operaciones largas) pero gestiona activamente el tamaño de las posiciones a través de una cascada de martingala y un reinicio basado en acciones. Se abre una nueva operación inmediatamente después de que se cierra la anterior, manteniendo la estrategia constantemente involucrada en el mercado.

## Lógica de trading
1. **Entradas secuenciales**: la estrategia abre una orden de mercado larga siempre que no haya ninguna posición activa. Las órdenes de stop-loss y take-profit se adjuntan inmediatamente después del cumplimiento.
2. **Contabilidad de pérdidas y ganancias**: después de cerrar una posición, el precio realizado se compara con el precio de entrada. Un cierre rentable incrementa el contador de ganancias; de lo contrario, se incrementa el contador de pérdidas. Los resultados del punto de equilibrio se tratan como pérdidas y coinciden con el EA original.
3. **Finalización del conjunto**: también se realiza un seguimiento del número de operaciones en el conjunto actual. Cuando el contador llega a `Trades Per Set`, el ciclo se considera completo y puede ocurrir uno de tres resultados:
   - **Todas las ganancias**: el volumen se vuelve a calcular a partir del capital actual usando `Equity Divisor` y los contadores de ciclo se reinician.
   - **Todas las pérdidas**: el volumen se multiplica por `Scale Factor` y los contadores de ciclos se reinician.
   - **Resultados mixtos**: si el conjunto contiene tanto victorias como derrotas, los contadores simplemente se reinician y se conserva el volumen actual.
4. **Reinicio del capital**: cada vez que el capital de la cartera crece al menos `Equity Increase`, la estrategia realiza un reinicio global. Se borran todos los contadores, el volumen base se vuelve a calcular a partir del capital y el objetivo de capital avanza en el mismo incremento.

Este comportamiento refleja el EA original donde los bloques comerciales se encadenaban a través de nodos lógicos de fxDreema.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Trades Per Set` | Número de operaciones secuenciales que forman un ciclo de martingala. |
| `Stop Loss (pips)` | Distancia de stop-loss medida en pasos de precio del instrumento. Establezca en cero para desactivar. |
| `Take Profit (pips)` | Distancia de obtención de beneficios medida en incrementos de precios. Establezca en cero para desactivar. |
| `Scale Factor` | Multiplicador aplicado al volumen comercial después de una serie totalmente perdedora. Los valores inferiores a 1 se fijan automáticamente en 1. |
| `Equity Divisor` | Divide el capital de la cuenta para obtener el tamaño del lote base después de un conjunto totalmente ganador o un reinicio del capital. |
| `Equity Increase` | Cantidad de crecimiento del capital que desencadena el reinicio global. Establezca en cero para deshabilitar la salida basada en acciones. |

## Gestión monetaria
- El volumen está alineado con las restricciones del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) de la misma manera que el EA original.
- Cuando los datos de acciones no están disponibles, el volumen anterior se reutiliza y vuelve a `VolumeStep` si se trata de la primera operación.
- Las distancias de stop-loss y take-profit se convierten en incrementos de precio a través de `PriceStep`. Si el instrumento no especifica un incremento de precio, el valor bruto se redondea al entero más cercano.

## Notas de uso
- La estrategia es solo larga, al igual que el script MetaTrader. Si el corredor admite cortocircuitos, deshabilítelo manualmente cuando ejecute la estrategia.
- Debido a que las órdenes de parada y objetivo se recrean después de cada ejecución, las ejecuciones parciales se manejan con elegancia: el volumen restante hereda las mismas órdenes de protección.
- El reinicio del capital se evalúa después de cada posición cerrada. Asegúrese de que la conexión de la cartera proporcione valores de acciones actuales para que se pueda alcanzar el umbral de reinicio.
