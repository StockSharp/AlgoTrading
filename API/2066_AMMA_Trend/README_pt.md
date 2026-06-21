# Estratégia AMMA Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia usa o indicador **Modified Moving Average (AMMA)** para capturar mudanças de tendência de curto prazo. Analisa a direção da inclinação do AMMA nas velas recentes e abre uma posição na direção da tendência emergente enquanto fecha a oposta.

## Como funciona

1. Uma `ModifiedMovingAverage` com período configurável é calculada no período selecionado.
2. Em cada vela concluída, a estratégia compara os últimos três valores do AMMA.
3. Se os valores do indicador formarem uma sequência ascendente e o valor mais recente for maior que o anterior, uma posição comprada é aberta. Qualquer posição vendida é fechada.
4. Se os valores do indicador formarem uma sequência descendente e o valor mais recente for menor que o anterior, uma posição vendida é aberta. Qualquer posição comprada é fechada.

## Parâmetros

- `CandleType` – período das velas utilizadas para cálculos.
- `MaPeriod` – período da média móvel modificada.
- `AllowLongEntry` – habilitar abertura de posições compradas.
- `AllowShortEntry` – habilitar abertura de posições vendidas.
- `AllowLongExit` – habilitar fechamento de posições compradas.
- `AllowShortExit` – habilitar fechamento de posições vendidas.

## Notas

A estratégia opera apenas em velas completas e utiliza os métodos integrados `BuyMarket` e `SellMarket` para execução de ordens. Funções de gestão de risco podem ser adicionadas externamente usando as propriedades padrão de `Strategy`.
