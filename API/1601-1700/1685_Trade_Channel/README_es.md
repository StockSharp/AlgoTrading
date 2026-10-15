# Estrategia de Canal de Negociación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Canal de Negociación** opera rompimientos y retrocesos alrededor de un canal de precios Donchian. Cuando la banda superior permanece sin cambios y el precio la toca o cierra por debajo de ella pero por encima del pivote, se abre una posición larga. La lógica opuesta se utiliza para entradas cortas. El stop loss se coloca más allá de la banda opuesta por el valor del ATR. Un trailing stop opcional puede ajustar el stop a medida que la operación avanza en beneficio.

## Parámetros

- `ChannelPeriod` — longitud del canal Donchian.
- `AtrPeriod` — período ATR para el cálculo del stop loss.
- `Trailing` — distancia del trailing stop en unidades de precio (0 desactiva el trailing).
- `CandleType` — tipo de vela utilizado para los cálculos.
