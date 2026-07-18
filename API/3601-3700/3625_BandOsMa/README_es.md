# Estrategia BandOsMa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia BandOsMa** convierte el asesor experto MetaTrader 5 "BandOsMA" en una estrategia StockSharp. Evalúa el histograma MACD (OsMA) utilizando Bollinger bandas construidas directamente sobre los valores del histograma. Las rupturas por encima o por debajo de las bandas crean señales de entrada, mientras que una media móvil adicional del histograma gestiona las salidas de señales.

La estrategia opera con un solo símbolo y período de tiempo seleccionado por el usuario. Los valores del indicador se calculan sobre velas terminadas utilizando las suscripciones de velas de alto nivel de StockSharp.

## Lógica de trading
1. **Indicadores**
   - `MovingAverageConvergenceDivergenceSignal` proporciona el histograma MACD (OsMA).
   - `BollingerBands` se aplica a la secuencia OsMA para detectar desviaciones extremas.
   - Una media móvil configurable suaviza el histograma y actúa como filtro de salida.
2. **Entrada**
   - Aparece una **señal larga** cuando el OsMA actual cierra por debajo de la banda inferior mientras que la barra anterior permaneció por encima de ella.
   - Aparece una **señal corta** cuando el OsMA actual cierra por encima de la banda superior mientras que la barra anterior permaneció por debajo de ella.
3. **Salir**
   - Las señales se borran cuando el histograma cruza la media móvil en la dirección opuesta.
   - Cuando una posición abierta ya no coincide con la señal activa, la posición se cierra inmediatamente.
   - Se adjunta un stop-loss basado en pips a cada posición. La parada también actúa como una parada de seguimiento con la misma distancia y un paso de seguimiento igual a `StopLossPoints / 50` (reflejando la clase auxiliar MetaTrader.

## Gestión de Puestos
- **Stop Loss & Trailing**: La distancia del stop se expresa en MetaTrader puntos y se convierte en unidades de precio utilizando el `PriceStep` del instrumento. La misma distancia se utiliza para el trailing stop, que avanza una vez que el precio de cierre mejora al menos el paso de seguimiento.
- **Una posición a la vez**: Solo se mantiene una posición neta. Las señales opuestas cierran la posición actual antes de considerar una nueva entrada.

## Parámetros
| grupo | Nombre | Descripción | Predeterminado |
| --- | --- | --- | --- |
| generales | `CandleType` | Plazo para la suscripción de velas y cálculo del indicador. | `H1` |
| Riesgo | `LotSize` | Volumen comercial en lotes. | `0.01` |
| Riesgo | `StopLossPoints` | Distancia de stop-loss expresada en MetaTrader puntos (también utilizada para seguimiento). | `1000` |
| Indicadores | `MacdFastPeriod` | Longitud rápida de EMA en MACD. | `12` |
| Indicadores | `MacdSlowPeriod` | Longitud lenta de EMA en MACD. | `26` |
| Indicadores | `MacdSignalPeriod` | Longitud de la señal EMA en MACD. | `9` |
| Indicadores | `PriceType` | Precio aplicado para la entrada MACD (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Typical` |
| Indicadores | `BollingerPeriod` | Período de Bollinger Bandas sobre la secuencia OsMA. | `26` |
| Indicadores | `BollingerShift` | Cambio aplicado a Bollinger buffers (no negativo). | `0` |
| Indicadores | `BollingerDeviation` | Multiplicador de desviación estándar para Bollinger bandas. | `2` |
| Indicadores | `MovingAveragePeriod` | Longitud de la media móvil aplicada a OsMA. | `10` |
| Indicadores | `MovingAverageShift` | Cambio aplicado al buffer de media móvil (no negativo). | `0` |
| Indicadores | `MovingAverageMethod` | Tipo de media móvil (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |

## Notas de implementación
- El procesamiento de velas utiliza `WhenCandlesFinished` para garantizar que solo las barras finales impulsen la lógica.
- Los valores del indicador se almacenan en búferes históricos para emular cambios de búfer estilo MetaTrader. No se admiten cambios negativos; utilice valores cero o positivos como en los valores predeterminados originales del experto.
- Las paradas dinámicas se basan en cierres de velas en lugar de actualizaciones paso a paso. Ajuste la distancia del pip si se requiere un seguimiento preciso del nivel de tick.

## Uso
1. Seleccione el símbolo y el período de tiempo deseados en StockSharp.
2. Configure los parámetros, especialmente `CandleType`, `LotSize` y períodos del indicador.
3. Iniciar la estrategia; se suscribirá a velas, calculará los indicadores y ejecutará operaciones de acuerdo con la lógica descrita.
