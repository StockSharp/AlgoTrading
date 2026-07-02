# Estratégia de rótulos de lucro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de rótulos de lucro** converte o MetaTrader 5 consultor especialista *Rótulos de lucro (54352)* em StockSharp API de alto nível. A estratégia monitora os cruzamentos da média móvel exponencial tripla (TEMA) para abrir posições e desenha rótulos de lucro no gráfico após o fechamento de uma posição. Quando a tendência sobe, o algoritmo abre uma posição longa e, quando a tendência cai, ele abre uma posição curta. Se uma posição oposta ainda estiver ativa, a estratégia primeiro a fecha e imprime a etiqueta de lucro realizado.

As velas são processadas por meio de uma assinatura `SubscribeCandles`, e o indicador é vinculado por meio de `Bind` para manter a implementação totalmente de alto nível. As velas finalizadas atualizam os valores TEMA e acionam decisões de negociação.

## Regras de negociação

1. **Cruzamento de alta**: quando o valor TEMA atual se move acima do valor anterior enquanto as leituras mais antigas mostram uma inclinação descendente, a estratégia abre uma posição longa se nenhuma posição longa estiver ativa no momento.
2. **Cruzamento de baixa**: quando o TEMA cai da mesma maneira, ele abre uma posição curta se nenhuma venda estiver ativa.
3. **Reversão de posição**: se existir uma posição oposta no momento de um novo sinal, a estratégia fecha a posição aberta antes de colocar uma nova ordem.
4. **Rótulos de lucro**: assim que a posição for totalmente fechada, o PnL realizado é calculado e exibido no gráfico usando `DrawText`.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Período usado para assinatura de velas. |
| `TemaPeriod` | `6` | Período da média móvel exponencial tripla. |
| `TradeVolume` | `0.1` | Volume enviado com cada ordem de mercado. |
| `PlacingTrade` | `false` | Ativa ou desativa a colocação de pedidos em tempo real. |
| `LabelOffset` | `0` | Deslocamento vertical aplicado ao rótulo de lucro acima do preço comercial. |

## Notas

- A estratégia depende apenas de velas finalizadas e não acessa diretamente os buffers do indicador.
- Os níveis protetores de stop-loss e take-profit da versão MQL não são replicados; as posições são invertidas quando chega um sinal oposto.
- Os rótulos de lucro usam a moeda de segurança sempre que ela está disponível e, caso contrário, voltam aos valores brutos.
