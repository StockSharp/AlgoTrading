# Estratégia de Operação Automática com Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza Bollinger Bands, RSI e o oscilador estocástico para abrir negociações automaticamente durante uma janela de tempo GMT especificada. Uma posição vendida é aberta quando a vela anterior fecha acima da banda superior das Bollinger Bands com o RSI acima de 75 e o %K do estocástico acima de 85. Uma posição comprada é aberta quando a vela fecha abaixo da banda inferior com o RSI abaixo de 25 e o %K do estocástico abaixo de 155. Apenas uma posição por direção é permitida. Um trailing stop em pontos protege as posições abertas.

## Parâmetros

- `OpenBuy` – habilitar a abertura de posições compradas.
- `OpenSell` – habilitar a abertura de posições vendidas.
- `GmtTradeStart` – hora de início de negociação em GMT (exclusiva).
- `GmtTradeStop` – hora de fim de negociação em GMT (exclusiva).
- `BbPeriod` – período para Bollinger Bands.
- `RsiPeriod` – período para o indicador RSI.
- `StochKPeriod` – período %K para o oscilador estocástico.
- `StochDPeriod` – período %D para o oscilador estocástico.
- `StochSlowing` – fator de suavização para o oscilador estocástico.
- `TrailingStop` – distância do trailing stop em pontos.
- `CandleType` – período de velas utilizado para os cálculos.
