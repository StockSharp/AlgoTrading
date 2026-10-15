# Estrategia de Cambio de Rango XD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Cambio de Rango XD recrea el expert advisor de MetaTrader 5 **Exp_XD-RangeSwitch** usando la API de alto nivel de StockSharp. Se basa en el indicador de canal personalizado XD-RangeSwitch, que traza bandas superiores e inferiores alternantes junto con flechas cada vez que la banda dominante cambia. La estrategia puede ya sea desvanecer esas flechas (comportamiento contra-tendencia) o operar en la dirección del rompimiento dependiendo del parámetro `TradeDirection`. El dimensionamiento de órdenes sigue la configuración base `Strategy.Volume`, mientras que las fórmulas originales de gestión de dinero son reemplazadas por los helpers de gestión de posición de StockSharp.

## Recreación del indicador XD-RangeSwitch
* El indicador rastrea las últimas `Peaks` velas completadas para determinar los rangos más altos y más bajos.
* Se imprime un canal alcista (banda inferior) cuando el cierre actual está por encima del máximo más alto de las `Peaks` barras previas. Su valor equivale al mínimo más bajo en la misma ventana más la barra actual.
* Se imprime un canal bajista (banda superior) cuando el cierre actual está por debajo del mínimo más bajo de las `Peaks` barras previas. Su valor equivale al máximo más alto en la misma ventana más la barra actual.
* Si no ocurre ningún rompimiento, los valores previos del canal se propagan hacia adelante.
* Cada vez que un canal reaparece después de estar vacío, la estrategia registra una señal de flecha en el precio del canal. Esto refleja el comportamiento de los buffers 2 y 3 de MT5 utilizados por el expert original.
* Solo se procesan velas completamente terminadas, asegurando valores consistentes en ejecuciones en vivo e históricas.

## Lógica de trading
1. La estrategia procesa velas del marco temporal seleccionado por `CandleType` y almacena los buffers de indicadores reconstruidos.
2. Para cada nueva vela, inspecciona el valor del indicador que tiene `SignalBar` velas de antigüedad (el código MT5 usa el mismo desplazamiento al llamar a `CopyBuffer`).
3. El mapeo de señales depende de la opción `TradeDirection`:
   * **AgainstSignal** replica el comportamiento predeterminado de MT5 — las flechas alcistas activan largos y también solicitan cerrar operaciones cortas; las flechas bajistas activan cortos y solicitan cerrar largos.
   * **WithSignal** invierte la interpretación, por lo que las flechas alcistas se tratan como puntos de salida para largos y puntos de entrada para cortos, operando efectivamente en la misma dirección que el rompimiento del canal.
4. Los buffers de tendencia sin flechas aún se respetan como señales de salida, coincidiendo con los indicadores originales `SELL_Close` y `BUY_Close`.
5. Los cierres siempre se ejecutan antes que las aperturas, permitiendo que la estrategia aplane una posición opuesta antes de entrar en la nueva dirección.
6. Las órdenes se envían con helpers de ejecución de mercado (`BuyMarket`/`SellMarket`). Cuando ocurre un cambio mientras una posición opuesta está abierta, la cantidad solicitada se aumenta automáticamente para compensar completamente la exposición antes de establecer la nueva posición.

## Gestión del riesgo
* La lógica opcional de stop-loss y take-profit se proporciona a través de los parámetros `UseStopLoss`/`StopLossPoints` y `UseTakeProfit`/`TakeProfitPoints`.
* Las distancias se miden en unidades de precio absolutas, reflejando las entradas de "puntos" en el script MT5.
* Los stops y objetivos se evalúan en cada vela terminada usando los valores máximos/mínimos de la vela para emular la activación dentro de la barra.
* Si tanto un stop como un objetivo están activos, el stop tiene prioridad — la posición se cierra una vez que se alcanza cualquiera de los niveles.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | Velas H4 | Marco temporal utilizado para los cálculos de XD-RangeSwitch. |
| `Peaks` | 4 | Número de picos (longitud de lookback) analizados por el indicador. |
| `SignalBar` | 1 | Número de barras completadas hacia atrás al leer los buffers del indicador. |
| `TradeDirection` | AgainstSignal | Elegir entre interpretación contra-tendencia o seguimiento de tendencia de las señales. |
| `AllowBuyEntry` / `AllowSellEntry` | true | Habilitar o deshabilitar la apertura de nuevas posiciones en la dirección correspondiente. |
| `AllowBuyExit` / `AllowSellExit` | true | Permitir que la estrategia cierre posiciones existentes cuando el indicador lo solicita. |
| `UseStopLoss` / `StopLossPoints` | true / 1000 | Activar el manejo del stop-loss y definir su distancia en unidades de precio. |
| `UseTakeProfit` / `TakeProfitPoints` | true / 2000 | Activar el manejo del take-profit y definir su distancia en unidades de precio. |

## Notas
* Los buffers de máximos/mínimos se mantienen internamente dentro de la estrategia en lugar de depender de colecciones de StockSharp, manteniéndose fiel a la implementación de MT5 y adhiriéndose a las pautas de conversión.
* Las señales se evalúan solo en velas terminadas. Si `SignalBar` es mayor que cero, la orden se coloca en la siguiente vela después de la que produjo la señal, como en el expert de MT5.
* Los valores del indicador se mantienen en un historial rodante corto que se extiende apenas más allá del mayor entre `Peaks` y `SignalBar`, asegurando un uso determinista de memoria incluso durante simulaciones largas.
* La configuración predeterminada refleja los valores predeterminados de MT5: velas H4, `Peaks = 4`, `SignalBar = 1`, trading contra-tendencia y un sobre de riesgo de 1.000/2.000 puntos.
