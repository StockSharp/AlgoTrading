# VIDYA N Barras Fronteras Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia original MetaTrader combina el indicador de canal "VIDYA N Bars Borders" con un módulo de dimensionamiento de posición martingala. El puerto StockSharp mantiene la idea de comprar cuando el precio cae por debajo de la banda inferior adaptativa y vender cuando el precio sube por encima de la banda superior. El centro del canal es producido por una media móvil adaptativa (análogo de VIDYA) y su ancho está controlado por una envolvente de rango verdadero promedio. Un bloque de administración de dinero aumenta el tamaño de la operación después de perder operaciones mientras se respetan los límites máximos de posición y exposición.

## Lógica comercial
1. Suscríbase a las velas de período de tiempo seleccionadas.
2. Calcule una media móvil adaptativa de Kaufman como sustituto de VIDYA y un canal ATR a su alrededor.
3. Cuando el cierre de una vela terminada cruce por debajo de la banda inferior, abra o invierta a una posición larga (a menos que la bandera `Reverse` esté habilitada, en cuyo caso se abre una venta corta).
4. Cuando el cierre cruce por encima de la banda superior, abra o invierta a una posición corta (o larga si `Reverse` es verdadero).
5. Haga cumplir una distancia de precio mínima entre entradas consecutivas para evitar volver a ingresar demasiado cerca del llenado anterior.
6. Si el beneficio flotante en la posición abierta alcanza el objetivo monetario especificado, aplana todo y espera la siguiente señal.
7. Después de cada operación cerrada, el siguiente volumen base se restablece al tamaño inicial (después de una operación rentable) o se multiplica por el índice martingala (después de una operación perdedora). El volumen resultante se alinea con el paso del instrumento y se aplican límites de volumen total y por operación.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Candle Type` | Tipo de datos de velas para negociar. |
| `CMO Period` | Ventana de ratio de eficiencia para la media móvil adaptativa. |
| `EMA Period` | Período de suavizado de la media móvil adaptativa. |
| `ATR Period` | Número de barras para el ancho medio del canal ATR. |
| `Profit Target` | Umbral de beneficio monetario que desencadena una salida total. |
| `Increase Ratio` | Multiplicador aplicado al siguiente volumen comercial después de una operación perdedora. |
| `Max Position Volume` | Techo rígido para un único volumen de pedido/posición. |
| `Max Total Volume` | Límite superior de la exposición total abierta por la estrategia. |
| `Max Positions` | Número máximo de posiciones concurrentes (el puerto mantiene una posición neta). |
| `Minimum Step` | Distancia mínima entre dos entradas consecutivas, medida en puntos. |
| `Base Volume` | Tamaño inicial del pedido antes de los ajustes de martingala. |
| `Reverse Signals` | Invierte la interpretación larga/corta de la ruptura del canal. |

## Notas de implementación
- StockSharp no incluye una implementación directa de VIDYA. La estrategia utiliza `KaufmanAdaptiveMovingAverage` con eficiencia configurable y ventanas de suavizado para imitar el comportamiento adaptativo de VIDYA. Esto mantiene la capacidad de respuesta cerca del indicador original mientras se basa en componentes integrados.
- Sólo se gestiona una posición neta a la vez. La versión MetaTrader puso en cola varias entradas pendientes; en StockSharp cada señal abre una nueva posición o invierte la actual. La escala Martingale se aplica al siguiente tamaño de entrada en lugar de agregar nuevas capas inmediatamente.
- La alineación mínima de pasos y volúmenes depende de los metadatos del instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`). Proporcione estos valores al configurar la estrategia para obtener límites de ejecución precisos.
- El seguimiento de beneficios se basa en la estrategia `PnL` y el último cierre de vela, lo cual es suficiente para realizar pruebas retrospectivas de alto nivel. Para operar en vivo, conecte la estrategia a una cartera que actualice los valores PnL realizados.

## Archivos
- `CS/VidyaNBarsBordersMartingaleStrategy.cs` — Implementación en C# de la estrategia.
