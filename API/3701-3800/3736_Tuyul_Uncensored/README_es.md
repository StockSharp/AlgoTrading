# Estrategia sin censura de Tuyul
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Tuyul Uncensored es una estrategia de seguimiento de swing que reconstruye el asesor experto original MetaTrader 5 con el API de alto nivel de StockSharp. El sistema observa las oscilaciones del ZigZag, alinea las entradas con un filtro de tendencia de media móvil y coloca órdenes limitadas en el retroceso del 57% Fibonacci del último tramo. Cuando el precio vuelve a ese nivel, la estrategia intenta unirse a la oscilación dominante mientras protege la operación con niveles de limitación de pérdidas y toma de ganancias derivados del mismo tramo.

## Lógica de trading
1. **Preparación de datos**
   - Una suscripción de vela definida por el parámetro `Candle Type` seleccionado.
   - Se utiliza un indicador ZigZag (Profundidad/Desviación/Retroceso) para rastrear el último máximo y mínimo confirmados.
   - Los EMA rápidos y lentos (predeterminado 21/9) proporcionan el filtro direccional.
2. **Detección de señal**
   - Cuando el ZigZag confirma un nuevo pivote (ya sea un nuevo máximo o un nuevo mínimo), la estrategia actualiza el par de oscilación más reciente.
   - Si no hay órdenes activas y no hay ninguna posición abierta, los valores EMA anteriores determinan la tendencia:
     - EMA rápida por encima de EMA lenta → contexto alcista.
     - EMA rápida por debajo de EMA lenta → contexto bajista.
3. **Realización de pedidos**
   - En un contexto alcista, la estrategia coloca una orden de **límite de compra** en el retroceso del 57% entre el último mínimo y el último máximo.
   - En un contexto bajista, coloca una orden de **límite de venta** en el retroceso simétrico del 57% desde el máximo hasta el mínimo.
   - El stop-loss está anclado en el extremo opuesto del ZigZag, mientras que el take-profit es igual a la distancia del stop multiplicada por `Take Profit Multiplier` (predeterminado 1.2).
   - Las órdenes permanecen activas para `Wait Bars After Signal` velas; luego se cancelan a la espera de una nueva señal.
4. **Gestión de posiciones**
   - Una vez que se completa una orden, la estrategia observa las velas posteriores. Una posición larga se cierra cuando el precio alcanza el límite de pérdidas o la toma de ganancias predefinidos. La misma lógica reflejada se aplica a las posiciones cortas.
   - El comercio puede limitarse a días laborables específicos. Fuera de los días permitidos, todas las órdenes pendientes se eliminan, pero las posiciones existentes se dejan intactas, siguiendo el comportamiento original del asesor.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `Volume Per Trade` | Volumen de pedidos enviado con cada entrada. |
| `TP Multiplier` | Multiplicador aplicado a la distancia de parada para calcular la compensación de la toma de ganancias. |
| `ZigZag Depth` | Número de velas examinadas al confirmar una oscilación. |
| `ZigZag Deviation` | Desviación mínima (en puntos) requerida antes de que ZigZag valide un nuevo pivote. |
| `ZigZag Backstep` | Número mínimo de velas entre pivotes ZigZag opuestos. |
| `Wait Bars After Signal` | Velas máximas para mantener viva la orden pendiente antes de la cancelación. |
| `Fast EMA` | Período de la media móvil exponencial rápida utilizado como filtro de tendencia. |
| `Slow EMA` | Período de la media móvil exponencial lenta utilizado como filtro de tendencia. |
| `Allow Monday … Allow Friday` | Alterna que habilitan o deshabilitan el comercio en días laborables individuales. |
| `Candle Type` | Serie de velas utilizada para todos los cálculos de indicadores y decisiones comerciales. |

## Notas
- La tasa de retroceso Fibonacci se fija en 57% como en la fuente EA.
- Los niveles de stop-loss y take-profit se monitorean al cierre de las velas; Los picos intrabar más allá de los umbrales desencadenan salidas del mercado en la siguiente evaluación.
- La estrategia mantiene una única orden pendiente a la vez, reflejando la implementación original.
