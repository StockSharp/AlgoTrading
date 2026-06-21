# Estratégia Color Zerolag Momentum OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói um oscilador OSMA de Momentum de zero defasagem personalizado usando cinco cálculos de Momentum.
Quando o valor do oscilador de dois períodos atrás está abaixo do valor de três períodos atrás, a tendência é considerada ascendente.
Neste caso, as posições vendidas são fechadas e uma nova posição comprada pode ser aberta se o valor mais recente estiver acima do valor de dois períodos atrás.
Quando o valor de dois períodos atrás está acima do valor de três períodos atrás, a tendência é descendente, as posições compradas são fechadas e uma vendida pode ser aberta se o último valor estiver abaixo do valor de dois períodos atrás.

## Parâmetros

- `Smoothing1` – primeiro fator de suavização para a tendência lenta.
- `Smoothing2` – segundo fator de suavização para a linha OSMA.
- `Factor1-5` – pesos aplicados a cada componente de Momentum.
- `MomentumPeriod1-5` – períodos para os indicadores de Momentum.
- `CandleType` – período de candles para cálculos.
- `BuyOpen` – permitir abertura de posições compradas.
- `SellOpen` – permitir abertura de posições vendidas.
- `BuyClose` – permitir fechamento de posições compradas.
- `SellClose` – permitir fechamento de posições vendidas.
