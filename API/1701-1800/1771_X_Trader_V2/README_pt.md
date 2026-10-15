# Estratégia X Trader V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um sistema de cruzamento de médias móveis contrário, convertido do especialista MQL4 original **X_trader_v2**. Utiliza duas médias móveis para detectar reversões repentinas e executa operações na direção oposta ao cruzamento.

## Como funciona
1. Duas médias móveis simples são calculadas no período selecionado.
2. Quando a MA rápida cruza **acima** da MA lenta, a estratégia abre uma posição **vendida**.
3. Quando a MA rápida cruza **abaixo** da MA lenta, a estratégia abre uma posição **comprada**.
4. Apenas uma posição pode estar aberta por vez. Uma nova operação é colocada somente após o fechamento da anterior e o aparecimento de um novo sinal.
5. A proteção integrada coloca automaticamente ordens de stop-loss e take-profit.

## Parâmetros
- `Ma1Period` – período da média móvel rápida.
- `Ma2Period` – período da média móvel lenta.
- `TakeProfitTicks` – distância do take-profit em ticks de preço.
- `StopLossTicks` – distância do stop-loss em ticks de preço.
- `CandleType` – tipo de vela utilizado nos cálculos.

## Observações
- A estratégia assina dados de velas por meio da API de alto nível.
- Os valores dos indicadores são processados via bindings sem chamadas diretas a `GetValue`.
- O algoritmo armazena internamente os valores anteriores dos indicadores para evitar consultas extensas ao histórico.
