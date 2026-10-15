# Estrategia Adaptativa Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

Esta estrategia es un port a StockSharp del asesor experto de MetaTrader 5 *Perceptron.mq5*.  
Cinco señales discretas de indicadores se combinan a través de un perceptrón de dos capas. Cada operación registra el estado del indicador y, una vez cerrada la posición, los pesos sinápticos se refuerzan o penalizan dependiendo del beneficio obtenido. El comportamiento imita el bucle de autoaprendizaje del EA original aprovechando la API de velas de alto nivel de StockSharp.

## Capa de indicadores

| Código | Descripción | Lógica de señal |
| --- | --- | --- |
| `IND1` | Cruce de medias móviles simples rápida/lenta | +1 cuando la MA rápida cruza por encima de la MA lenta en la barra anterior, −1 en un cruce descendente, de lo contrario 0. |
| `IND2` | Índice de Fuerza Relativa (RSI) | +1 cuando el RSI sale de la zona de sobrevendida (cruza por encima de 30), −1 cuando el RSI sale de la zona de sobrecomprada (cruza por debajo de 70). |
| `IND3` | Índice del Canal de Materias Primas (CCI) | +1 en un cruce por encima de −100, −1 en un cruce por debajo de +100. |
| `IND4` | Pendiente de la media móvil simple corta | +1 si la MA corta aumentó entre las dos barras anteriores, −1 si disminuyó. |
| `IND5` | Color del momentum del Awesome Oscillator | +1 cuando el histograma aumenta en comparación con el valor anterior (color alcista), −1 cuando disminuye. |

Todos los indicadores se evalúan en velas cerradas. Se mantienen búferes históricos internamente para replicar el windowing `CopyBuffer` utilizado en el script MQL5.

## Arquitectura del perceptrón

- Cinco neuronas ocultas (`NN1`…`NN5`) combinan cuatro indicadores cada una, imitando el cableado en el EA.
- Cada neurona tiene su propio diccionario de pesos sinápticos más un peso de sesgo (`NNS1`…`NNS5`).
- La activación final `brainReturn` es la suma ponderada de las salidas de las neuronas.  
  - `brainReturn > 0` → solicitar una entrada larga (si la operación anterior tampoco fue larga).  
  - `brainReturn < 0` → solicitar una entrada corta (si la operación anterior tampoco fue corta).
- Las posiciones se abren solo con órdenes de mercado cuando no hay posición activa.

## Gestión de posición

- El precio de entrada, dirección y estados de indicador/neurona se capturan en cada ejecución.
- Los desplazamientos de take-profit y stop-loss se aplican en unidades de precio absoluto (p. ej. 0.0004 para 4 puntos en un par Forex con 5 decimales).  
  Cuando se abre una nueva vela tras la entrada:
  - Para largos, primero se compara el máximo con el precio de take-profit, luego el mínimo con el stop-loss.  
  - Para cortos, primero se compara el mínimo con el precio de take-profit, luego el máximo con el stop-loss.  
  - Si ambos niveles se superan dentro de la misma vela, el take-profit tiene prioridad, coincidiendo con el comportamiento optimista del EA original.
- Una vez detectada una salida, la estrategia cierra la posición con una orden de mercado y calcula el beneficio realizado usando el nivel TP/SL correspondiente.

## Actualización adaptativa de pesos

Cuando se cierra una operación, los estados de indicador y neurona capturados se replayan:

1. Se determina `directionSign` (−1 para largos, +1 para cortos) y `outcomeSign` (signo del PnL realizado).
2. Los pesos de sesgo se ajustan dentro de `[SinMin, SinMax]`:
   - Si `sign(neuronOutput) * directionSign` es positivo, el sesgo sigue el resultado de la operación (aumenta en ganancias, disminuye en pérdidas).
   - De lo contrario, el sesgo se mueve en sentido opuesto al resultado.
3. Los pesos sinápticos se comportan de manera similar pero permanecen sin límites: las señales alineadas con la dirección de la posición reciben refuerzo en ganancias y penalizaciones en pérdidas, mientras que las señales opuestas hacen lo inverso.
4. Las señales almacenadas se borran para evitar el uso accidental.

Esto generaliza las más de 1.500 líneas de gestión condicional de sinapsis del EA en una rutina de refuerzo compacta.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | Marco temporal de 1 minuto | Suscripción de velas utilizada por la estrategia. |
| `FastMaLength` | 5 | Período de la SMA rápida en la señal de cruce. |
| `SlowMaLength` | 9 | Período de la SMA lenta. |
| `RsiLength` | 14 | Período de cálculo del RSI. |
| `CciLength` | 14 | Período de cálculo del CCI. |
| `SlopeMaLength` | 5 | Período de la MA utilizada para la detección de pendiente. |
| `AoShortLength` | 5 | Período corto del Awesome Oscillator. |
| `AoLongLength` | 34 | Período largo del Awesome Oscillator. |
| `StopLossOffset` | 0.001 | Distancia de stop-loss en unidades de precio absoluto (0 deshabilita el stop). |
| `TakeProfitOffset` | 0.0004 | Distancia de take-profit en unidades de precio absoluto (0 deshabilita el objetivo). |
| `SinMax` | 5 | Límite superior para los pesos de sesgo neuronal. |
| `SinMin` | 0 | Límite inferior para los pesos de sesgo neuronal. |
| `SinPlusStep` | 0.03 | Incremento de refuerzo positivo. |
| `SinMinusStep` | 0.03 | Decremento de refuerzo negativo. |

Todos los parámetros numéricos están expuestos como `StrategyParam<T>` y pueden optimizarse en StockSharp Designer.

## Notas de implementación

- Usa la API de suscripción de velas de alto nivel con vinculación multi-indicador.
- Se emplea gestión manual de operaciones para que los precios realizados sean conocidos al actualizar las sinapsis.
- Los historiales de indicadores se almacenan con campos anulables para asegurar que las señales solo se activen después de la formación completa.
- El búfer de color del Awesome Oscillator en el EA se aproxima comparando los valores actuales y anteriores del histograma.
- La salida de gráfico dibuja la serie de velas más las medias móviles rápida y lenta. Los marcadores de operaciones muestran el comportamiento adaptativo en tiempo real.

## Limitaciones y supuestos

- Los stops y objetivos se evalúan una vez por vela completada; el orden intrabar de los eventos es desconocido, por lo que se da prioridad al objetivo de ganancia cuando se alcanzan ambos umbrales.
- Los pesos de indicadores no están acotados como en el EA original y pueden crecer considerablemente durante ciclos de refuerzo prolongados.
- El `LastTradeType` del EA original nunca se reiniciaba; en este port se borra después de cada salida para que las operaciones consecutivas en la misma dirección sigan siendo posibles.
