# Estratégia CM Fishing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia CM Fishing** é uma abordagem de negociação em grade adaptada do script MQL original `cm_fishing.mq4`. A estratégia abre ordens a mercado sempre que o preço se move um número fixo de pontos a partir da última operação executada. Ela pode construir uma grade de posições compradas ou vendidas e fechá-las quando um alvo de lucro especificado é atingido.

Esta implementação foca na lógica central de negociação sem a interface gráfica do script original. As ordens são executadas usando a API de alto nível da StockSharp.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Buy` | Habilita ou desabilita a abertura de posições compradas. |
| `Sell` | Habilita ou desabilita a abertura de posições vendidas. |
| `StepBuy` | Passo de preço em pontos que deve ser percorrido para baixo antes de abrir uma nova posição comprada. |
| `StepSell` | Passo de preço em pontos que deve ser percorrido para cima antes de abrir uma nova posição vendida. |
| `CloseProfitBuy` | Limiar de lucro para fechar todas as posições compradas. |
| `CloseProfitSell` | Limiar de lucro para fechar todas as posições vendidas. |
| `CloseProfit` | Limiar de lucro que fecha qualquer posição aberta independentemente da direção. |
| `BuyVolume` | Volume da ordem para cada operação comprada. |
| `SellVolume` | Volume da ordem para cada operação vendida. |

## Lógica de negociação

1. Rastrear preços de operações em tempo real.
2. Quando o preço cai `StepBuy` a partir do último nível de operação e `Buy` está habilitado, enviar uma ordem de compra a mercado.
3. Quando o preço sobe `StepSell` a partir do último nível de operação e `Sell` está habilitado, enviar uma ordem de venda a mercado.
4. Manter o preço médio de entrada da posição atual.
5. Fechar posições quando o lucro não realizado exceder o parâmetro `CloseProfit*` correspondente.

A estratégia trabalha com dados de tick e é adequada para fins de demonstração e educacionais.

## Notas

- A implementação não reproduz a interface do usuário do script original.
- Apenas uma posição líquida (comprada ou vendida) é mantida a qualquer momento.
