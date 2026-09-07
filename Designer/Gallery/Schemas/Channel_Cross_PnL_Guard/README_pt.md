# Diagrama da estratégia de cruzamento de canal com proteção de P&L
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama calcula o ponto médio de um canal de 24 candles a partir de candles concluídos de cinco minutos do BTCUSDT@BNBFT e negocia cruzamentos exatos entre o fechamento e o ponto médio após uma pausa de 200 candles. A profundidade de mercado do TONUSDT@BNBFT confirma que o fluxo está pronto e marca as verificações de P&L não realizado; uma proteção monetária de disparo único solicita o cancelamento de qualquer ordem de entrada ainda ativa e fecha a posição em BTC quando qualquer limite configurado é alcançado.

![schema](schema.svg)

## Visão geral da estratégia

- A variável BTC Security configura a assinatura de candles concluídos de cinco minutos. Ordens a mercado, execuções, fechamento de posição e P&L pertencem ao Strategy Security selecionado, que deve ser definido como BTCUSDT@BNBFT para corresponder a BTC Security.
- Highest(24) recebe os candles de BTC e acompanha suas máximas, enquanto Lowest(24) acompanha suas mínimas. O ponto médio aritmético é `(Highest + Lowest) / 2`; a primeira decisão disponível armazena fechamento e ponto médio, inicia a primeira pausa e não envia uma ordem.
- Um cruzamento para cima exige `Previous Close <= Previous Midpoint` e `Current Close > Current Midpoint`. Um cruzamento para baixo exige `Previous Close >= Previous Midpoint` e `Current Close < Current Midpoint`. Os valores armazenados avançam a cada candle concluído de BTC, inclusive quando a pausa rejeita a ação.
- Uma entrada ou reversão só fica elegível após 200 candles concluídos de BTC estritamente subsequentes e pelo menos um evento de profundidade de mercado de TON. Cada cruzamento aceito reinicia o atraso de 200 candles antes do envio da ordem a mercado.
- Um registro com sinal atualizado pelas execuções mantém o estado gerenciado de BTC: `-1` é vendido, `0` é sem posição e `1` é comprado. P&L não realizado igual ou superior a `500`, ou igual ou inferior a `-300`, dispara a proteção uma única vez; uma execução posterior de compra ou venda volta a armá-la.

## Regras de entrada e saída

- **Entrada comprada**: Quando ocorre o cruzamento exato para cima, o registro com sinal está sem posição ou vendido, ambos os portões de prontidão estão abertos e a pausa terminou, envia uma compra a mercado. A entrada sem posição usa Base BTC Volume; a reversão de vendido para comprado usa o dobro dessa quantidade.
- **Entrada vendida**: Quando ocorre o cruzamento exato para baixo, o registro com sinal está sem posição ou comprado, ambos os portões de prontidão estão abertos e a pausa terminou, envia uma venda a mercado. A entrada sem posição usa Base BTC Volume; a reversão de comprado para vendido usa o dobro dessa quantidade.
- **Saída**: Um cruzamento oposto elegível executa uma reversão comum em uma etapa, em vez de um fechamento separado. De forma independente, a proteção de P&L dispara em `P&L >= Profit Target` ou `P&L <= -abs(Maximum Loss)`, envia uma intenção de cancelamento em massa e solicitações direcionadas de cancelamento para as ordens de entrada armazenadas, e então solicita fechamento a mercado. Esse fechamento monetário não reinicia a pausa de cruzamentos.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| BTC Security | BTCUSDT@BNBFT | Instrumento usado pela assinatura de candles de cinco minutos. Defina Strategy Security com o mesmo valor, pois ações de ordem, execuções, fechamento de posição e P&L usam Strategy Security. |
| TON Readiness Security | TONUSDT@BNBFT | Instrumento usado apenas pela assinatura de profundidade de mercado que abre o portão de prontidão do fluxo e marca a amostragem de P&L; suas cotações não são usadas nas ordens de BTC nem na avaliação de P&L. |
| Candle Series | 00:05:00 | Série de candles concluídos de cinco minutos do BTCUSDT usada no canal, nas decisões de cruzamento exato, na contagem da pausa e no gráfico. |
| Highest Length | 24 | Quantidade de candles de BTC usada por Highest para calcular o limite superior do canal. |
| Lowest Length | 24 | Quantidade de candles de BTC usada por Lowest para calcular o limite inferior do canal. |
| Base BTC Volume | 1 | Quantidade padrão da entrada a mercado. A fórmula é `Base BTC Volume * (1 + abs(latch))`, portanto uma entrada sem posição usa a quantidade base e uma reversão usa o dobro. |
| Cooldown N | 200 | Quantidade de candles concluídos de BTC estritamente subsequentes exigida após a inicialização ou um cruzamento aceito antes que outro cruzamento possa enviar uma ordem. |
| Profit Target | 500 | Nível de P&L não realizado no qual, ou acima do qual, a proteção de disparo único solicita cancelamento e fechamento a mercado. |
| Maximum Loss | 300 | Magnitude positiva da perda; o limite da proteção é calculado como `-abs(Maximum Loss)`, resultando em `-300` por padrão. |

## Detalhes do diagrama

- A [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) BTC alimenta somente a assinatura de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) concluídos. A variável TON alimenta apenas a [Profundidade de mercado](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html); seu primeiro evento grava o registro de prontidão e os eventos seguintes também marcam a amostragem do último P&L não realizado.
- Dois blocos de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) recebem cada candle de BTC. Highest(24), Lowest(24), uma fórmula de ponto médio e registros de valores atuais e anteriores preservam uma decisão completa de fechamento e canal por candle concluído.
- Um bloco Delay começa na primeira decisão e reinicia a cada cruzamento aceito. Como o candle atual chega à sua entrada antes da execução do ramo de decisão, a elegibilidade só retorna após 200 candles concluídos de BTC posteriores; cruzamentos rejeitados ainda substituem o fechamento e o ponto médio armazenados.
- Os blocos de [Registro de ordens](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/register.html) de compra e venda enviam ordens a mercado com `Base BTC Volume * (1 + abs(latch))`. Suas saídas MyTrade gravam o estado com sinal e voltam a armar a proteção de P&L a partir de execuções reais.
- Eventos de mudança de P&L e de profundidade de TON amostram o último P&L não realizado. Comparações estritas dos limites alimentam um portão armado compartilhado, de modo que alcançar `500` ou `-300` só pode produzir uma ação protetora até que uma execução de entrada posterior volte a armá-lo.
- A proteção chama o [Cancelamento em massa de ordens](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html), libera as referências Order de compra e venda armazenadas para blocos de cancelamento direcionado e usa Base BTC Volume para fechar a mercado a exposição atual de BTC. O gráfico recebe candles de BTC, Highest(24), Lowest(24), ponto médio, P&L, ordens enviadas e todas as execuções da estratégia.

## Uso

Importe o arquivo `.json` no Designer, defina Strategy Security como BTCUSDT@BNBFT, execute-o no backtester com histórico de candles e profundidade de mercado e depois ajuste os parâmetros ou blocos ao seu instrumento antes de negociar ao vivo.
