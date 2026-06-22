# Estratégia XOSignal Re-Open
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o especialista do MetaTrader *Exp_XOSignal_ReOpen* dentro do StockSharp usando a API de alto nível. Opera com dados de velas do símbolo e período selecionados com um detector de rompimento estilo XO construído sobre ATR(13). Quando uma seta para cima aparece, o algoritmo fecha os vendidos, opcionalmente abre um comprado, e então adiciona à posição cada vez que o preço progride um número fixo de ticks. Setas para baixo se comportam simetricamente para vendidos. Stops duros e alvos em ticks são aplicados a cada camada da pirâmide.

## Lógica central

- A estratégia calcula um canal de intervalo XO cujas bandas se expandem em `Range * PriceStep`. Os rompimentos reiniciam as bandas e estabelecem a direção de tendência atual.
- ATR(13) controla quão abaixo/acima da vela os níveis de entrada virtual (setas) são traçados: setas longas aparecem em `Low - ATR * 3/8`, setas curtas em `High + ATR * 3/8`.
- Apenas velas completas são processadas. Os sinais podem ser atrasados em `SignalBar` barras para imitar a lógica de buffering original.

## Regras de entrada

- **Entrada comprada**: quando uma seta para cima é emitida, entradas compradas são permitidas (`EnableBuyEntries = true`), nenhuma posição vendida está aberta, e o sinal ainda não foi executado. O volume da operação é igual a `Volume`.
- **Reentrada comprada**: enquanto em uma posição comprada, cada `PriceStepTicks` ticks adicionais a favor da operação (com base no fechamento da vela) aciona outra compra até que `MaxPyramidingPositions` camadas sejam abertas. Cada reentrada atualiza os níveis de stop/alvo protetores.
- **Entrada/reentrada vendida**: lógica espelho do lado comprado usando a seta para baixo.

## Regras de saída

- **Saídas baseadas em sinal**: uma seta para cima fecha cada vendido ativo quando `EnableSellExits = true`; uma seta para baixo fecha o comprado quando `EnableBuyExits = true`.
- **Saídas de risco**: cada camada aberta carrega a mesma distância de stop-loss e take-profit definida em ticks (`StopLossTicks`, `TakeProfitTicks`). Quando o preço viola o nível dentro da vela atual, toda a posição é zerada.
- **Zerar manualmente**: sinais de entrada opostos também neutralizam a direção anterior antes de abrir uma nova posição.

## Gestão de posição

- O tamanho da posição é fixo em `Volume` para cada ordem.
- Stop-loss e take-profit são medidos em ticks do instrumento. Defini-los como zero desabilita a proteção correspondente.
- O contador de pirâmide reinicia para zero após qualquer saída completa para que o próximo sinal comece a partir de uma posição base nova.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Volume` | Tamanho de ordem para cada entrada | `1` |
| `StopLossTicks` | Distância de stop em ticks, 0 desabilita | `1000` |
| `TakeProfitTicks` | Distância de take-profit em ticks, 0 desabilita | `2000` |
| `PriceStepTicks` | Movimento favorável mínimo antes de adicionar à posição | `300` |
| `MaxPyramidingPositions` | Número máximo de entradas em camadas (incluindo a primeira) | `10` |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir abrir posições compradas/vendidas | `true` |
| `EnableBuyExits` / `EnableSellExits` | Permitir fechar posições compradas/vendidas em setas opostas | `true` |
| `CandleType` | Período usado para sinais | `H4` |
| `Range` | Altura da caixa XO em ticks | `10` |
| `AppliedPrice` | Fonte de preço usada no detector XO | `Close` |
| `SignalBar` | Número de barras fechadas para atrasar sinais | `1` |

A estratégia é projetada para backtesting ou operativa ao vivo com instrumentos que fornecem um passo de preço confiável. Ajustar as distâncias baseadas em ticks para corresponder à volatilidade do mercado selecionado.
