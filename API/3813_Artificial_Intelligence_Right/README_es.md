# Estrategia correcta de inteligencia artificial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto MetaTrader 4 **ArtificialIntelligence_Right.mq4**. Evalúa una sola capa.
perceptrón construido sobre el oscilador de Aceleración/Desaceleración (AC) para decidir cuándo el impulso del mercado cambia de dirección. el
El perceptrón utiliza cuatro muestras de CA retardadas y las convierte en una señal firmada que impulsa tanto las entradas como las reversiones.

A diferencia del EA original, el puerto StockSharp funciona en la vela de alto nivel API. Las acciones de precio se toman al cierre de cada
vela terminada, que mantiene la lógica determinista para pruebas retrospectivas y flujos de trabajo de optimización.

## Indicadores y cálculos
- **El oscilador de aceleración/desaceleración** se reconstruye a partir del Awesome Oscillator restando un SMA de 5 períodos del AO
valores ({PH000}} de 5 períodos de `HL2` menos SMA de 34 períodos de `HL2`).
- Un búfer circular almacena los 22 valores de CA más recientes para que el perceptrón pueda acceder a las compensaciones 0, 7, 14 y 21, coincidiendo exactamente
la implementación MetaTrader.
- Los pesos del perceptrón se desplazan `-100` antes del producto escalar, reproduciendo la lógica `w = x - 100` del código fuente.

## Reglas de trading
1. **Condiciones de entrada**
   - Cuando la producción del perceptrón es positiva y la estrategia es plana, se envía una orden de compra de mercado.
   - Cuando la producción del perceptrón es negativa y la estrategia es plana, se envía una orden de venta de mercado.
2. **Gestión de stop loss**
   - Se asigna una parada de protección virtual después de cada entrada a una distancia igual a `StopLossPoints * PriceStep` del
precio de entrada. Esto emula el multiplicador `Point` de MetaTrader.
   - Si el precio de cierre cruza este nivel, la posición se cierra en el mercado para imitar la ejecución de la orden stop-loss.
3. **Seguimiento y reversión**
   - Una vez que la posición flota en ganancias por `(2 * StopLossPoints + SpreadPoints)` puntos, el robot original comienza
siguiendo la parada por la distancia de parada-pérdida o se invierte si el perceptrón cambia de signo.
   - La versión StockSharp utiliza el mismo disparador: cuando se alcanza el umbral de ganancias, si el perceptrón invierte la dirección,
se emite una orden de mercado con el doble de la exposición actual para revertir la operación; de lo contrario, la parada virtual se arrastra hasta
preservar la distancia original desde el cierre actual.

Todas las reversiones se realizan negociando el doble del volumen abierto, de modo que la posición resultante refleje el MetaTrader `OrderCloseBy`
comportamiento, terminando en la dirección opuesta pero con el mismo tamaño de lote.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `X1` … `X4` | Pesos del perceptrón. Los valores predeterminados replican la fuente `.mq4` (135, 127, 16, 93). |
| `StopLoss` | Distancia de stop-loss expresada en MetaTrader puntos. Se multiplica por el instrumento `PriceStep` para obtener una compensación de precio real. |
| `Spread` | Búfer de dispersión adicional (3 puntos predeterminados) utilizado en la condición de activación final. |
| `Candle Type` | Serie de velas utilizadas para los cálculos. El valor predeterminado es un período de tiempo de 1 minuto. |

La propiedad `Volume` está preestablecida en 1 lote, reflejando el parámetro de entrada `lots` del experto original.

## Notas de implementación
- Los cálculos del indicador y el estado del perceptrón se restablecen cada vez que se restablece la estrategia para evitar que los valores obsoletos causen
falsos desencadenantes.
- Si el valor no proporciona un `PriceStep`, la estrategia vuelve a un valor en puntos de `1`, manteniendo la compatibilidad.
con instrumentos genéricos de backtesting.
- No se registran órdenes stop reales; en cambio, la lógica de parada se ejecuta mediante órdenes de mercado en el controlador de velas. Esto mantiene el
comportamiento consistente entre corredores y simuladores.
