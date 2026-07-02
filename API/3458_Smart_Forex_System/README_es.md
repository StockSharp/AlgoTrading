# Estrategia del sistema Forex inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Smart Forex System es una versión StockSharp del asesor experto MetaTrader "Smart Forex System". El robot combina un filtro de impulso de una sola vela con una cuadrícula de promedio estilo martingala. La primera operación se abre cuando la vela anterior muestra un cierre direccional fuerte y el precio actual se ha alejado lo suficiente del cierre de referencia. Se agregan entradas adicionales a intervalos de pips fijos en la dirección adversa, y el tamaño de la posición aumenta mediante un multiplicador configurable. La estrategia gestiona las salidas a través de niveles promedio de toma de ganancias y un stop-loss de seguridad vinculado a la última orden de la red.

## Lógica de trading
- **Generación de señal**
  - Evalúe la última vela completa en el período de tiempo seleccionado.
  - Calcule una relación de impulso: `(current close - previous close) / previous close * 10,000`.
  - Si la vela anterior es bajista y el impulso es inferior al umbral negativo, puede comenzar una cesta larga.
  - Si la vela anterior es alcista y el impulso supera el umbral positivo, puede comenzar una cesta corta.
  - El comercio puede limitarse a solo largo, solo corto, en ambas direcciones o deshabilitarse por completo a través del parámetro `Trading Mode`.
- **Expansión de la red**
  - Una vez que existe una cesta, se agregan nuevas entradas cada vez que el precio se mueve contra la posición por al menos `Grid Step` pips desde el precio de la última orden.
  - Cada nuevo volumen de pedido se multiplica por `Lot Multiplier`. Los volúmenes están sujetos a los límites del corredor y al `Max Volume` configurado.
  - La cesta deja de crecer cuando el número de pedidos llega a `Max Trades`.
- **Gestión de salida**
  - Se coloca un stop-loss estricto a `Stop Loss` pips del precio de la última orden. Al superar esa distancia se cierra toda la canasta.
  - Los niveles de obtención de beneficios dependen del tamaño de la cesta:
    - Una sola orden utiliza `First Take Profit` pips del precio de entrada promedio ponderado por volumen.
    - Varias órdenes utilizan `Grid Take Profit` pips del mismo precio de entrada promedio para capturar rebotes más pequeños.
  - Las salidas se procesan en velas terminadas para garantizar que los indicadores tengan valores finales.

## Notas de gestión de riesgos
- El tamaño de la posición tipo martingala aumenta drásticamente la exposición a tendencias adversas. Utilice multiplicadores y tamaños de cesta conservadores en instrumentos altamente volátiles.
- El stop-loss predeterminado (400 pips) es intencionalmente amplio para reflejar el EA original. Considere alinearlo con el ATR del instrumento si se requieren pérdidas menores.
- El comercio en red consume margen rápidamente. Asegúrese de que el apalancamiento de la cuenta, el tamaño del contrato y los parámetros `Start Volume` sean consistentes con las especificaciones del corredor.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| Modo de negociación | Dirección comercial permitida (solo larga, solo corta, ambas o deshabilitada). | Largo y corto |
| Umbral de impulso | Impulso mínimo en pseudo-pips necesario para activar una señal. | 1 |
| Volumen inicial | Volumen del primer pedido en una cesta nueva. | 0,01 |
| Volumen máximo | Límite estricto aplicado a cualquier volumen de pedido único. | 2 |
| Multiplicador de lote | Multiplicador utilizado al dimensionar pedidos de cuadrícula posteriores. | 1.5 |
| Paso de cuadrícula | Distancia mínima en pips antes de agregar la siguiente orden. | 26 |
| Operaciones máximas | Número máximo de pedidos permitidos por dirección. | 12 |
| Primera toma de ganancias | Distancia de obtención de beneficios en pips cuando solo hay una orden abierta. | 30 |
| Toma de ganancias de la red | Distancia de obtención de beneficios en pips una vez que la cesta contiene varias órdenes. | 7 |
| Detener pérdidas | Distancia de parada en pips desde el último precio de la orden. | 400 |
| Tipo de vela | Plazo utilizado para la evaluación de la señal. | velas de 1 hora |

## Uso recomendado
1. Adjunte la estrategia a un símbolo de divisas con suficiente liquidez y un diferencial predecible.
2. Configure el `Candle Type` para que coincida con el período operativo del EA original (H1 por defecto) o adáptelo a su horizonte preferido.
3. Optimice el filtro de espacio, multiplicador y impulso de la red en datos históricos antes de la implementación en vivo.
4. Supervise de cerca el uso del margen. La canasta puede crecer rápidamente, así que considere combinar la estrategia con una protección de capital en toda la cuenta.
5. Evite la superposición con otros sistemas basados en redes en el mismo instrumento para reducir el riesgo de caídas compuestas.

## Diferencias en comparación con la versión MetaTrader
- El puerto StockSharp funciona con velas terminadas en lugar de actualizaciones paso a paso, lo que reduce el ruido y hace que la lógica sea determinista.
- Los volúmenes de pedidos se ajustan utilizando metadatos de seguridad StockSharp (mínimo, máximo y paso), lo que garantiza la compatibilidad con una amplia gama de corredores.
- Las comprobaciones de toma de ganancias y límite de pérdidas se manejan dentro de la lógica de la estrategia en lugar de enviar modificaciones de órdenes individuales para cada nivel de la red.
