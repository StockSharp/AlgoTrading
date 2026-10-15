# Estratégia de Alertas de Saída Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando a linha %K do Stochastic cruza acima de %D na zona de sobrevenda, e entra vendido quando %K cruza abaixo de %D na zona de sobrecompra. As posições são protegidas por stop loss fixo e take profit medidos em ticks. Quando ocorre um cruzamento oposto fora da zona extrema, a posição é fechada sem reverter.

## Parâmetros
- `StochLength` – período principal do oscilador Stochastic.
- `KLength` – período de suavização da linha %K.
- `DLength` – período de suavização da linha %D.
- `StopLossTicks` – distância do stop loss em ticks.
- `TakeProfitTicks` – distância do take profit em ticks.
- `CandleType` – período de tempo das velas.
