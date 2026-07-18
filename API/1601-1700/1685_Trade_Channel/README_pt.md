# Estratégia de Canal de Negociação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Canal de Negociação** opera rompimentos e retrocessos ao redor de um canal de preços Donchian. Quando a banda superior permanece inalterada e o preço a toca ou fecha abaixo dela mas acima do pivô, uma posição comprada é aberta. A lógica oposta é usada para entradas vendidas. O stop loss é colocado além da banda oposta pelo valor do ATR. Um trailing stop opcional pode apertar o stop conforme a operação avança em lucro.

## Parâmetros

- `ChannelPeriod` — comprimento do canal Donchian.
- `AtrPeriod` — período ATR para cálculo do stop loss.
- `Trailing` — distância do trailing stop em unidades de preço (0 desativa o trailing).
- `CandleType` — tipo de candle utilizado para os cálculos.
