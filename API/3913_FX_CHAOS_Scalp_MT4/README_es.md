# Estrategia FX-CHAOS Scalp MT4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia FX-CHAOS Scalp MT4 es una adaptación directa del asesor experto MetaTrader 4 que combina un filtro Awesome Oscillator con niveles de ZigZag construidos sobre fractales. La versión StockSharp mantiene el diseño de múltiples marcos temporales del sistema original: las velas horarias generan señales comerciales mientras que las velas diarias proporcionan un sesgo de marco temporal más alto. Dos rastreadores integrados reconstruyen el indicador "ZigZag en Fractals" escaneando patrones de cinco velas y registrando máximos y mínimos alternos.

## Flujo de trabajo comercial
1. **Recopilación de datos**
   - Las velas horarias alimentan la lógica de ejecución primaria y los controles de riesgo.
   - Las velas diarias actualizan la oscilación del ZigZag a largo plazo utilizada como filtro de tendencia.
   - El Awesome Oscillator (5, 34) se evalúa en la serie horaria a través del indicador de alto nivel API.
2. **Reconstrucción en zigzag**
   - Cada vela terminada se guarda en una ventana corrediza de cinco elementos.
   - Cuando la vela del medio forma un fractal ascendente, el rastreador guarda la vela alta como la última oscilación y cambia la dirección a "arriba"; un fractal descendente hace lo mismo con los mínimos.
   - Las oscilaciones consecutivas en la misma dirección sólo se reemplazan si el nuevo extremo es más pronunciado, imitando la lógica de amortiguación del indicador MT4.
3. **Detección de señal**
   - El búfer de ruptura agrega dos compensaciones de pasos de precio al máximo/mínimo de la hora anterior, reflejando el relleno `2*Point` que se encuentra en el código original.
   - Para entradas largas, la vela debe abrirse por debajo del máximo amortiguado, cerrar por encima de él, permanecer por debajo de la oscilación horaria más reciente del ZigZag, cerrar por encima de la última oscilación diaria y mantener el Oscilador Impresionante negativo.
   - Las entradas cortas reflejan las condiciones que utilizan los valores del oscilador positivo, el nivel inferior de ZigZag superior y el buffer.
4. **Ejecución de órdenes y resolución de conflictos**
   - Las posiciones opuestas se cierran antes de enviar una nueva orden, por lo que la estrategia nunca mantiene operaciones largas y cortas simultáneas.
   - El precio de cierre ejecutado se almacena para derivar distancias de stop-loss y take-profit en velas posteriores.

## Gestión del riesgo
- Los umbrales de limitación de pérdidas y obtención de beneficios son opcionales; un valor de `0` deshabilita la regla correspondiente.
- Al final de cada vela terminada, la estrategia comprueba si el rango de la vela ha tocado el stop o el objetivo configurado y cierra la posición si se ha superado el nivel.
- Cuando aparece una ruptura opuesta, la posición se liquida primero y luego la nueva operación se envía en la misma vela para preservar la regla de posición única.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen comercial en lotes aplicado a cada orden de mercado. |
| `Stop Loss (pts)` | Distancia en puntos para el tope de protección. Multiplicado por el paso del precio del valor. Establezca en `0` para desactivar. |
| `Take Profit (pts)` | Distancia en puntos para el objetivo de beneficio. Multiplicado por el paso de precio. Establezca en `0` para desactivar. |
| `Breakout Buffer` | Se agregaron puntos adicionales al extremo de la vela anterior antes de probar las rupturas. El valor predeterminado reproduce el colchón `2*Point` utilizado en MT4. |
| `Spread (pts)` | Spread promedio en puntos que se agrega al umbral de ruptura en las señales de compra para que la entrada refleje `2*Point + spread` de MT4. |
| `Trading Candle` | Plazo principal utilizado para las entradas (el valor predeterminado es una hora). |
| `Daily Candle` | Se utiliza un período de tiempo más alto para el filtro ZigZag (el valor predeterminado es un día). |

## Notas de implementación
- La estrategia se basa en los `SubscribeCandles` API y `BindEx` de alto nivel para evitar trabajar con buffers de indicadores directamente, respetando las pautas del repositorio.
- El paso de precio recuperado de `Security.PriceStep` se utiliza para convertir valores de parámetros expresados en puntos en distancias de precios absolutas. Si al instrumento le falta un paso, el código vuelve a `1`.
- Ambos rastreadores de ZigZag se reinician en `OnReseted` y pausan las operaciones hasta que acumulan suficientes velas para determinar el primer movimiento. Esto evita entradas prematuras cuando falta el contexto histórico.
- La representación del gráfico dibuja las velas horarias, el Awesome Oscillator y las operaciones de estrategia para ayudar a comparar la implementación de StockSharp con la plantilla MT4.
