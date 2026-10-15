# Estratégia Step Stochastic Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia usa o indicador Step Stochastic (um oscilador personalizado baseado em ATR) para gerar sinais de reversão. Ela assina um período de tempo de vela selecionado pelo usuário e calcula linhas rápida e lenta do Step Stochastic escaladas de 0 a 100.

## Regras de entrada e saída
- **Entrada comprada:** A linha lenta está acima de 50 e a linha rápida cruza de cima para baixo a linha lenta.
- **Entrada vendida:** A linha lenta está abaixo de 50 e a linha rápida cruza de baixo para cima a linha lenta.
- **Saída comprada:** A linha lenta está abaixo de 50 e o fechamento de posições compradas é permitido.
- **Saída vendida:** A linha lenta está acima de 50 e o fechamento de posições vendidas é permitido.

## Parâmetros
- `KFast` – multiplicador para o canal rápido.
- `KSlow` – multiplicador para o canal lento.
- `CandleType` – período das velas.
- `AllowBuyOpen`, `AllowSellOpen`, `AllowBuyClose`, `AllowSellClose` – permissões para ações de negociação.
- `StopLoss`, `TakeProfit` – níveis de proteção opcionais em unidades de preço.

A estratégia chama `StartProtection` para aplicar stop-loss e take-profit quando especificados.

O `StepStochasticIndicator` é um port em C# do indicador MQL5 original e produz valores `Fast` e `Slow` para cada vela concluída.
