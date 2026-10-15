# Estrategia de histograma del día de apertura MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el experto MetaTrader "2 1000 1 0,7% 0,5 500lev st" ingresando una operación al comienzo de cada nuevo día de negociación y filtrando la dirección con la pendiente del histograma MACD. El sistema fue diseñado para velas por hora y se basa en parámetros fijos de administración de dinero convertidos a partir de la configuración original MQL.

## Lógica de trading
- La estrategia monitorea las velas cada hora y detecta la primera vela de cada nuevo día.
- Evalúa el histograma MACD en las dos velas completadas más recientes del día anterior.
- Si el histograma cae entre esas dos barras, el sistema abre una posición larga en la primera vela del nuevo día.
- Si el histograma aumentó, en su lugar abre una posición corta.
- Sólo puede haber una posición activa a la vez. Las señales opuestas cierran la operación actual antes de abrir la nueva dirección.

## Gestión del riesgo
- Distancia inicial de stop-loss: 875 puntos (convertidos a precio multiplicando por el paso del precio del instrumento).
- Distancia de obtención de beneficios: 510 puntos.
- Distancia del trailing stop: 2172 puntos. El stop sigue el precio más alto (largo) o más bajo (corto) alcanzado desde la entrada y anula el stop inicial cuando se vuelve más ajustado.
- La opción de equilibrio original se deshabilitó y, por lo tanto, se omitió aquí.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Serie de velas utilizadas por la estrategia (por hora por defecto). | velas de 1 hora |
| `MacdFastPeriod` | Período EMA rápida para el MACD. | 58 |
| `MacdSlowPeriod` | Período lento de EMA para el MACD. | 195 |
| `MacdSignalPeriod` | Período de línea de señal para el MACD. | 183 |
| `StopLossPoints` | Distancia de stop-loss expresada en puntos del instrumento. | 875 |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos. | 510 |
| `TrailingStopPoints` | Distancia del trailing stop en puntos. | 2172 |

## Notas
- La estrategia utiliza sólo velas completadas para evitar la anticipación intrabarra, reflejando la opción "Usar valor de barra anterior" del experto fuente.
- Las salidas finales y fijas se manejan internamente, por lo que las protecciones adicionales de cartera deben permanecer deshabilitadas para evitar el doble manejo de las paradas.
- La lógica supone que el corredor utiliza definiciones de puntos estándar (paso de precio). Ajuste los parámetros si el instrumento utiliza un tamaño de tick diferente.
