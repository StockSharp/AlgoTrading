# Estrategia de AeroSpine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de AeroSpine es una conversión del experto de MetaTrader **AEROSPINE.mq4**. Opera con un único símbolo e intenta capturar rupturas alejadas del precio de apertura diaria. El robot original fue diseñado para gráficos diarios mientras monitoreaba ticks; el port mantiene la idea de ruptura de apertura diaria pero se basa en velas terminadas suministradas por StockSharp.

## Lógica de trading
- Al inicio de cada día de trading, la estrategia almacena el precio de apertura diaria derivado de la primera vela del día.
- Las nuevas posiciones se evalúan solo después de la hora de entrada configurada. Las velas terminadas deben satisfacer un filtro de volumen mínimo y el spread actual debe estar por debajo del límite configurado.
- Si no hay posición abierta y no hay operación de recuperación pendiente:
  - Se abre una operación **larga** una vez que el máximo de la vela supera la apertura diaria en `EntryOffsetPips`.
  - Se abre una operación **corta** una vez que el mínimo de la vela rompe por debajo de la apertura diaria en `EntryOffsetPips`.
- Después de cualquier operación perdedora, la estrategia prepara una entrada de recuperación en la dirección opuesta. Las operaciones de recuperación usan `RecoveryOffsetPips` e incrementan el volumen añadiendo el volumen base al tamaño de la operación perdedora, replicando el dimensionamiento estilo martingala del experto MQL.
- Las posiciones abiertas se gestionan con tres mecanismos:
  - Un take-profit fijo en `TakeProfitPips` desde el precio de entrada.
  - Un disparador opcional de break-even que cierra la operación una vez que el precio retrocede a la distancia de break-even tras haberse movido a favor de la posición.
  - Una salida protectora si el precio regresa a la apertura diaria y la cruza por `ExitOffsetPips` contra la posición.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| **Candle Type** | Marco temporal de las velas de trabajo usadas para la evaluación de señales. |
| **Volume** | Tamaño base de la orden usado para primeras entradas y para construir el volumen de recuperación. |
| **Entry Hour** | Hora mínima (hora de la bolsa) cuando se pueden tomar nuevas entradas. |
| **Entry Offset** | Distancia en pips desde la apertura diaria que debe cruzarse para abrir la primera operación del día. |
| **Exit Offset** | Distancia en pips más allá de la apertura diaria usada para cerrar posiciones que reviertan por encima de la apertura. |
| **Recovery Offset** | Distancia en pips desde la apertura diaria requerida para desencadenar una operación de recuperación tras una pérdida. |
| **Take Profit** | Distancia fija de take-profit medida en pips desde el precio de entrada. |
| **Break Even** | Distancia en pips requerida para armar la salida de break-even. |
| **Use Break Even** | Habilita o deshabilita el bloque de gestión de break-even. |
| **Volume Filter** | Volumen mínimo de vela requerido para nuevas entradas, replicando la verificación original `Volume[0] > 10000`. |
| **Max Spread** | Rechaza nuevas entradas si el spread actual es más amplio que el valor permitido (convertido desde pips). |
| **Enable Recovery** | Habilita la lógica de recuperación en dirección opuesta tras una operación perdedora. |

## Notas sobre la conversión
- El EA original colocaba órdenes directamente en ticks mientras aplicaba un gráfico diario. El port emula esto con velas intradía: la apertura diaria se actualiza en la primera vela de cada día y las verificaciones de ruptura usan los máximos/mínimos de las velas.
- Todos los elementos de la interfaz de MetaTrader (etiquetas, cálculos de capital en múltiples símbolos, etc.) fueron eliminados. Solo se preservó la lógica de trading relevante para el símbolo actual.
- El break-even y las modificaciones de stop (`OrderModify`) se simulan mediante llamadas explícitas a `ClosePosition()` cuando se alcanzan los umbrales calculados.
- Los filtros de spread y volumen se mapean directamente a las verificaciones originales `MODE_SPREAD` y `Volume[0]`.
