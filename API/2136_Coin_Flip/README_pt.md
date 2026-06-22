# Estratégia de Cara ou Coroa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Cara ou Coroa** escolhe aleatoriamente ir comprado ou vendido em cada nova vela quando não há posição aberta. Após fechar uma posição, se a negociação terminou em perda, o tamanho da próxima negociação é aumentado usando um multiplicador de martingale. A estratégia fecha posições usando níveis fixos de take-profit e stop-loss definidos em passos de preço e pode opcionalmente seguir os lucros após uma distância especificada.

## Parâmetros

- `Volume` – tamanho base da ordem usado para a primeira tentativa.
- `Martingale` – multiplicador aplicado ao volume após uma negociação perdedora.
- `MaxVolume` – limite superior para o tamanho da posição após aumentos por martingale.
- `TakeProfit` – alvo de lucro em passos de preço.
- `StopLoss` – limite de perda em passos de preço.
- `TrailingStart` – distância em passos de preço onde o trailing se torna ativo.
- `TrailingStop` – distância do trailing stop em passos de preço.
- `CandleType` – período das velas usado para tomada de decisão.

## Como funciona

1. Em cada vela concluída, a estratégia verifica se há uma posição aberta.
2. Se existir uma posição, ela monitora o lucro ou perda usando o preço de fechamento atual. Uma vez que as condições de take-profit, stop-loss ou trailing stop sejam atendidas, a posição é fechada.
3. Quando não há posição aberta, uma moeda virtual é lançada:
   - Cara abre uma posição comprada.
   - Coroa abre uma posição vendida.
4. Se a negociação anterior foi uma perda, o volume é multiplicado por `Martingale`, mas limitado por `MaxVolume`.
5. O trailing stop é ativado uma vez que o preço se move `TrailingStart` na direção favorável.

## Notas

Este exemplo destina-se a fins educacionais para demonstrar como trabalhar com sinais aleatórios e dimensionamento de posições usando a API de alto nível do StockSharp.
