# Martin Para Pequeños Depósitos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el experto de promediado "Martin for small deposits" en StockSharp. Analiza 15 velas completadas y abre una posición solo cuando el cierre más nuevo está por debajo (para largos) o por encima (para cortos) del cierre registrado 14 barras antes. Todas las operaciones se ejecutan a mercado usando la API de estrategias de alto nivel, y la lógica se aplica una vez por vela terminada.

## Lógica de entrada
- Un buffer deslizante mantiene los últimos 15 cierres de velas completadas.
- Cuando no hay posiciones abiertas ni pendientes, la estrategia compara el cierre más reciente con el cierre de 14 barras atrás.
- Si el último cierre es más bajo, se inicia una cuadrícula larga; si es más alto, se inicia una cuadrícula corta.
- El volumen de la operación para la primera orden es igual a **Initial Volume**. Las entradas subsiguientes en el mismo lado usan el multiplicador de martingala antes de ser normalizadas al paso de volumen del instrumento.

## Gestión de posición
- Mientras existe una posición, la estrategia espera **Bars To Skip** velas terminadas antes de considerar otra operación de promediado.
- Se envían órdenes adicionales solo si el precio se mueve en contra de la dirección actual en al menos **Step (pips)**, convertidos a unidades de precio usando el tamaño de pip detectado.
- Cada ejecución actualiza estadísticas internas: volumen agregado, precio de entrada promedio, precio de entrada más bajo (para largos) o más alto (para cortos), y el precio del último llenado.
- El volumen nunca excede **Max Volume** ni el volumen máximo definido por el exchange. Si el tamaño normalizado cae por debajo del volumen mínimo permitido, la orden se omite.

## Condiciones de salida
- Cuando el beneficio neto no realizado (diferencia entre el cierre actual y el precio de entrada promedio, multiplicado por el volumen de la posición) supera **Min Profit**, todas las órdenes abiertas se aplanan.
- Si **Take Profit (pips)** es mayor que cero y el precio alcanza esa distancia desde la última entrada en la dirección favorable, se cierra toda la cuadrícula.
- Las solicitudes de cierre se rastrean; no se envían nuevas órdenes hasta que las órdenes de salida estén completamente llenadas. Después de alcanzar un estado plano, todos los contadores internos se reinician para que la próxima señal inicie una cuadrícula nueva.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| Initial Volume | 0.01 | Tamaño de lote base para la primera operación. |
| Take Profit (pips) | 65 | Distancia en pips desde el último llenado que activa una salida total. Use 0 para deshabilitar esta verificación. |
| Step (pips) | 15 | Movimiento adverso en pips requerido antes de promediar en la posición. |
| Bars To Skip | 45 | Número mínimo de velas terminadas a esperar entre órdenes de promediado. |
| Increase Factor | 1.7 | Multiplicador aplicado al volumen de operación cada vez que se añade una nueva orden en el mismo lado. |
| Max Volume | 6 | Límite superior para el volumen agregado (antes de la normalización por los límites del mercado). |
| Min Profit | 10 | Objetivo de beneficio usado para cerrar toda la cuadrícula cuando el beneficio neto supera esta cantidad. |
| Candle Type | 1 hora | Marco temporal usado para la suscripción de velas y los cálculos de señal. |

## Notas de implementación
- El tamaño de pip se deriva de `Security.PriceStep` y la precisión decimal. Para instrumentos cotizados con 3 o 5 decimales, el código multiplica el paso de precio por 10 para coincidir con el concepto MQL de un pip.
- El beneficio no realizado se aproxima a partir de diferencias de precio y no incluye ajustes de swap o comisión que estaban presentes en el experto original.
- Se omiten operaciones de promediado adicionales mientras las órdenes de salida están activas, preservando el flujo de ejecución secuencial de la lógica MQL original.
- Cuando **Step (pips)** es cero, la estrategia nunca promedia; cuando **Take Profit (pips)** es cero, solo la condición **Min Profit** cierra la cuadrícula.
