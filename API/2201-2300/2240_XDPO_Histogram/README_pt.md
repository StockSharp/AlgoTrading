# Estratégia de Histograma XDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia de Histograma XDPO adapta o especialista MQL5 original *Exp_XDPO_Histogram*. Ela constrói um oscilador de preço destendenciado com duplo suavizado (XDPO) a partir de preços de fechamento. O oscilador é obtido subtraindo uma média móvel do preço e suavizando essa diferença com uma segunda média móvel. A dinâmica do histograma fornece sinais para abertura e fechamento de operações.

## Lógica de negociação

- Quando o oscilador vira para cima, todas as posições vendidas são fechadas. Se o valor atual do oscilador superar o anterior, uma nova posição comprada é aberta.
- Quando o oscilador vira para baixo, todas as posições compradas são fechadas. Se o valor atual do oscilador estiver abaixo do anterior, uma nova posição vendida é aberta.
- Os cálculos são realizados apenas em velas concluídas.

## Parâmetros

- `FirstMaLength` – comprimento da primeira média móvel aplicada ao preço.
- `SecondMaLength` – comprimento da média móvel aplicada à diferença entre o preço e a primeira MA.
- `CandleType` – tipo de vela utilizado para todos os cálculos.

## Notas

- As médias móveis são implementadas com indicadores `SimpleMovingAverage`.
- A estratégia usa ordens de mercado (`BuyMarket` e `SellMarket`) e fecha posições opostas antes de abrir novas.
