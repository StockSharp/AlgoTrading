# Estrategia ZeeZee Level
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia ZeeZee Level replica el comportamiento del asesor experto original de MetaTrader "ZeeZee Level" usando la API de alto nivel de StockSharp. La estrategia analiza oscilaciones ZigZag en el marco temporal seleccionado y opera en la dirección del extremo más reciente. Las distancias de stop loss, take profit y trailing stop de protección se expresan en pips, y el tamaño de posición sigue una progresión de estilo martingala después de operaciones perdedoras.

## Lógica de trading

1. Las velas se suscriben usando el marco temporal definido por `CandleType`.
2. Un `ZigZagIndicator` con parámetros configurables de profundidad, desviación y backstep rastrea máximos y mínimos oscilantes.
3. Cuando no hay posición abierta, la estrategia compara la recencia del último máximo y mínimo ZigZag confirmados dentro de la ventana `ZigZagIdInterval`:
   - Si el último máximo oscilante es más reciente que el último mínimo oscilante, se abre una posición corta.
   - Si el último mínimo oscilante es más reciente que el último máximo oscilante, se abre una posición larga.
4. Se mantiene solo una posición a la vez. El volumen de entrada se redondea al paso de volumen del instrumento.
5. Después de abrir la posición, se adjuntan niveles de stop loss, take profit y trailing stop opcional usando las distancias en pips configuradas. El trailing stop sigue el precio extremo a medida que la operación se mueve a favor.
6. Las posiciones se cierran en cuanto se toca el nivel de stop loss o take profit. Cuando ambos niveles se alcanzan en la misma vela, el nivel más cercano al precio de entrada gana el desempate.
7. Después de cada salida, el volumen se reinicia al valor inicial en operaciones rentables o se multiplica por el factor martingala en operaciones perdedoras.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `ZigZagDepth` | Número de velas consideradas al buscar nuevos pivotes ZigZag. |
| `ZigZagDeviation` | Movimiento mínimo de precio (en pasos de precio) requerido para confirmar un nuevo pivote. |
| `ZigZagBackstep` | Número mínimo de barras antes de que el indicador pueda cambiar de dirección. |
| `ZigZagIdInterval` | Número máximo de barras usadas para mirar hacia atrás en busca de los últimos máximos y mínimos ZigZag. |
| `StopLossPips` | Distancia de stop loss en pips. Establecer en cero para desactivar. |
| `TakeProfitPips` | Distancia de take profit en pips. Establecer en cero para desactivar. |
| `TrailingStopPips` | Distancia de trailing stop en pips. Establecer en cero para desactivar. |
| `InitialVolume` | Volumen base de operación usado al inicio de un ciclo martingala. |
| `MartingaleMultiplier` | Factor aplicado al volumen de la siguiente operación después de una posición perdedora. |
| `CandleType` | Tipo de vela y marco temporal usado para el análisis. |

## Gestión monetaria

- Los volúmenes se alinean con el paso de volumen del instrumento y se limitan entre los límites mínimo y máximo del mercado.
- Las operaciones ganadoras reinician el volumen a `InitialVolume`, mientras que las perdedoras lo multiplican por `MartingaleMultiplier`.

## Gestión de riesgos

- Las distancias de stop loss, take profit y trailing stop se evalúan en cada vela cerrada.
- El trailing stop se mueve solo en la dirección de la operación y nunca retrocede.
- El trading se omite mientras la estrategia ya mantiene una posición o mientras las oscilaciones ZigZag no están disponibles dentro del intervalo configurado.

## Notas

- La estrategia usa solo velas cerradas para coincidir con el comportamiento del asesor experto original.
- Las conversiones de pips dependen del `PriceStep` del instrumento. Asegúrese de que los metadatos del instrumento estén cargados antes de iniciar la estrategia.
