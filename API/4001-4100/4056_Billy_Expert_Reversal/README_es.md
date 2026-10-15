# Estrategia de Billy Experto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertido del MetaTrader 4 experto original "Billy_expert.mq4".
- Estrategia de impulso de solo largo que espera cuatro máximos descendentes consecutivos y abre antes de entrar.
- Utiliza dos osciladores estocásticos (rápido en el marco temporal de negociación, lento en un marco temporal más alto) para confirmar que el impulso se está desplazando hacia arriba.
- Diseñado para pares de divisas al contado, pero se puede aplicar a cualquier instrumento que proporcione velas basadas en minutos.

## Lógica de señal
### Filtro de acción de precio
1. Evalúe las velas terminadas en el período principal.
2. Requiere cuatro velas consecutivas donde tanto el máximo como la apertura disminuyen. Esto recrea las comprobaciones MT4 `High[0] < High[1] < High[2] < High[3]` y `Open[0] < Open[1] < Open[2] < Open[3]`.
3. El patrón sugiere un movimiento bajista agotado y prepara la estrategia para una operación de reversión.

### Confirmación del oscilador
1. Calcule un oscilador estocástico rápido en el período de negociación y un estocástico lento en el período de confirmación.
2. Para cada oscilador, exija que la línea %K esté por encima de la línea %D tanto en la vela completa actual como en la anterior (`%K(0) > %D(0)` y `%K(1) > %D(1)`).
3. La operación se activa sólo cuando ambos osciladores confirman simultáneamente un impulso alcista.

## Gestión de pedidos
- Entradas: compras de mercado dimensionadas por el parámetro de estrategia `Volume` (si existe una posición corta, se cierra y se revierte automáticamente).
- Stop loss: distancia fija por debajo del precio de cumplimiento utilizando el parámetro `Stop Loss (pts)`. Un valor de `0` desactiva la parada.
- Take Profit: distancia fija por encima del precio de cumplimiento utilizando el parámetro `Take Profit (pts)`. Un valor de `0` desactiva el objetivo.
- Límite de posición: `Max Orders` limita cuántas entradas largas pueden estar activas al mismo tiempo. Debido a que StockSharp mantiene una posición neta, la estrategia se aproxima al comportamiento de MT4 al contar cuántos bloques `Volume` están abiertos actualmente.
- Trailing stop: el EA original declaró una entrada de trailing stop pero no la implementó. La versión convertida también omite la lógica final de paridad.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Trading Candle` | Marco temporal principal para el patrón de precios y el estocástico rápido. | 1 minuto |
| `Slow Stochastic Candle` | Marco de tiempo más alto utilizado para el estocástico de confirmación. | 5 minutos |
| `Stochastic Length` | Ventana retrospectiva para %K. | 5 |
| `%K Smoothing` | Suavizado aplicado a la línea %K. | 3 |
| `%D Period` | Suavizado aplicado a la línea %D. | 3 |
| `Slowing` | Factor de suavizado adicional para %K. | 3 |
| `Stop Loss (pts)` | Distancia de stop loss en pasos de precio. | 0 |
| `Take Profit (pts)` | Tome la distancia de beneficio en pasos de precio. | 12 |
| `Max Orders` | Máximo de entradas largas simultáneas. | 1 |

## Notas de uso
- Establezca la propiedad `Volume` antes de iniciar la estrategia; StockSharp tiene como valor predeterminado `0`, lo que bloquearía la realización de pedidos.
- El paso del precio se lee desde `Security.PriceStep` (vuelve a `Security.Step` o `1`). Asegúrese de que los metadatos de su instrumento estén configurados correctamente para obtener niveles de parada/objetivo precisos.
- Cuando el período de confirmación difiere del período de negociación, la vela lenta completada más recientemente se reutiliza hasta que aparece una nueva, que coincide con el comportamiento del script MT4 original.
- El EA no logró salidas más allá del stop loss y la toma de ganancias del lado del corredor. La conversión refleja este comportamiento al enviar órdenes de mercado protectoras cuando se tocan los niveles.
- Debido a que StockSharp agrega posiciones, `Max Orders > 1` funciona mejor cuando cada entrada usa el mismo tamaño `Volume`.

## Diferencias con la versión MT4
- Comprobación de seguridad para detectar información faltante sobre el paso del precio con una advertencia de registro en lugar de utilizar `Point` de forma silenciosa.
- Se agregaron cláusulas de protección para garantizar que la estrategia se opere solo cuando todos los datos requeridos (historial de precios y ambos osciladores estocásticos) estén disponibles.
- La estrategia se ejecuta únicamente con velas terminadas, mientras que MT4 procesó ticks pero se limitó por el tiempo de la barra. Este cambio evita evaluaciones duplicadas y mantiene la lógica determinista.
