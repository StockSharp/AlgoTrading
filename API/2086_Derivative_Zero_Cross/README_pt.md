# Estratégia de Cruzamento Zero da Derivada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na mudança de sinal da derivada do preço. A derivada é calculada como o momentum do preço dividido pelo período e multiplicado por 100. Quando a derivada cruza a linha zero, a posição atual é fechada e a posição oposta é aberta.

## Parâmetros

- `DerivativePeriod` - período de suavização para o cálculo da derivada.
- `PriceType` - preço fonte utilizado para a derivada.
- `BuyEntry` - permitir a abertura de posições compradas.
- `SellEntry` - permitir a abertura de posições vendidas.
- `BuyExit` - permitir o fechamento de posições compradas.
- `SellExit` - permitir o fechamento de posições vendidas.
- `StopLoss` - stop loss em pontos.
- `TakeProfit` - take profit em pontos.
- `CandleType` - período de tempo das velas.

## Lógica

1. Assinar velas e calcular o momentum do preço selecionado.
2. A derivada é obtida dividindo o momentum pelo período e escalando por 100.
3. Quando a derivada passa de positiva para não positiva, uma posição comprada é aberta e a vendida é fechada.
4. Quando a derivada passa de negativa para não negativa, uma posição vendida é aberta e a comprada é fechada.
5. Stop loss e take profit são aplicados para gestão de risco.
