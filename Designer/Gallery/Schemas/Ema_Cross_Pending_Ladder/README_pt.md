# Diagrama da estratégia de cruzamento de EMA com degrau de ordem pendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia cruzamentos confirmados de EMA com uma entrada em dois degraus. Uma ordem a mercado abre ou reverte totalmente a posição; após sua execução completa, uma ordem limite mais distante no mesmo sentido é colocada a partir do último BestBid; a proteção por distâncias absolutas de preço gerencia a exposição. Uma pausa de 100 candles suspende tanto novas entradas do primeiro degrau quanto as verificações de preço da proteção nos fechamentos finalizados.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam a EMA rápida 14 e a EMA lenta 50, que emitem somente valores formados. Crossing gera um evento de alta quando a EMA rápida passa acima da lenta e um evento de baixa quando cai abaixo dela.
- Um retrato da posição no instante do candle e o estado de prontidão filtram as entradas. O cruzamento para cima só permite compra com Position <= 0, e o cruzamento para baixo só permite venda com Position >= 0; o primeiro degrau a mercado usa Base Volume + abs(Position), abrindo a partir de zero ou revertendo totalmente a exposição contrária.
- Quando a ordem a mercado do primeiro degrau alcança Matched, o segundo é ancorado no último BestBid amostrado continuamente. O ramo comprado envia uma compra limite em BestBid - 100, e o ramo vendido uma venda limite em BestBid + 100, ambas com Base Volume 1 e ShrinkPrice desativado.
- A última ordem registrada do segundo degrau é cancelada por um cruzamento EMA contrário sem o filtro de entrada, uma execução protetora ou o fim da pausa. Uma execução do segundo degrau entra na proteção da posição, mas não reinicia a pausa.
- As execuções dos quatro blocos de ordens de entrada alimentam a proteção por distâncias absolutas com Take Distance 400 e Stop Distance 200. Enquanto o estado de prontidão está ativo, cada fechamento finalizado é verificado e uma saída acionada é enviada a mercado. Uma execução do primeiro degrau ou uma saída protetora inicia a pausa: os 100 candles finalizados seguintes não permitem uma nova entrada do primeiro degrau nem uma verificação de preço da proteção; ambas retornam no 101º. O gráfico mostra candles, ambas as EMAs, dois fluxos de ordens limite e cinco fluxos de negócios.

## Regras de entrada e saída

- **Entrada comprada**: No cruzamento da EMA rápida acima da lenta, se o retrato da posição for menor ou igual a zero e a pausa estiver pronta, o diagrama compra a mercado Base Volume + abs(Position). Depois da execução total da ordem, coloca uma compra limite com Base Volume no BestBid armazenado menos Rung Distance.
- **Entrada vendida**: No cruzamento da EMA rápida abaixo da lenta, se o retrato da posição for maior ou igual a zero e a pausa estiver pronta, o diagrama vende a mercado Base Volume + abs(Position). Depois da execução total da ordem, coloca uma venda limite com Base Volume no BestBid armazenado mais Rung Distance.
- **Saída**: A proteção da posição recebe as execuções dos dois degraus a mercado e dos dois degraus limite. Enquanto o estado de prontidão está ativo, os fechamentos de candles finalizados são verificados e a posição sai a mercado se o preço atingir a distância favorável de 400 unidades ou a desfavorável de 200. Durante os 100 candles finalizados após uma execução do primeiro degrau ou uma saída protetora não há verificações de preço da proteção; elas retornam no 101º. A execução protetora cancela o último segundo degrau pendente, e um cruzamento contrário sem filtro também o cancela independentemente da prontidão de entrada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Intervalo de cinco minutos; somente candles finalizados acionam o cálculo das EMAs, sinais, proteção e contagem da pausa. |
| Fast EMA Length | 14 | Comprimento da ExponentialMovingAverage rápida; somente valores formados são emitidos. |
| Slow EMA Length | 50 | Comprimento da ExponentialMovingAverage lenta; somente valores formados são emitidos. |
| Base Volume | 1 | Quantidade somada a abs(Position) no primeiro degrau a mercado e usada sem ajuste no segundo degrau limite. |
| Rung Distance | 100 price units | Deslocamento absoluto a partir do BestBid armazenado: subtraído para a compra limite e somado para a venda limite. |
| Cooldown | 100 candles | Número de candles finalizados posteriores em que tanto novas entradas do primeiro degrau quanto verificações de preço da proteção ficam bloqueadas; ambas retornam no 101º candle. |
| Take Distance | 400 price units | Movimento absoluto favorável do preço que aciona a saída protetora a mercado. |
| Stop Distance | 200 price units | Movimento absoluto desfavorável do preço que aciona a saída protetora a mercado. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite somente candles finalizados de cinco minutos. Dois blocos de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), limitados a valores formados, calculam ExponentialMovingAverage 14 e 50.
- [Crossing](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) emite true para eventos de alta e false para eventos de baixa; uma [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT torna o evento de baixa acionável. A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) é amostrada antes do caminho das EMAs, e blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) combinam Position <= 0 ou Position >= 0 com o estado de prontidão. As portas de entrada Long e Short encaminham somente pulsos true aos acionadores do primeiro degrau.
- Uma [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) calcula Base Volume + abs(Position). Os blocos de [Registro de ordem](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/register.html) do primeiro degrau enviam ordens a mercado NoCondition, e suas saídas Matched acionam o segundo degrau correspondente.
- Um bloco [Level1](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) contínuo fornece BestBid, retido por uma [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). Blocos de [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) de preço calculam BestBid - Rung Distance e BestBid + Rung Distance; os blocos de [Registro de ordem](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/register.html) do segundo degrau colocam ordens limite no mesmo sentido com Base Volume e ShrinkPrice false.
- Cada novo segundo degrau passa a ser a ordem retida para [Cancelar ordem](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html). O cancelamento é acionado pelo cruzamento direto para o outro lado, por uma execução protetora ou pelo fim da pausa. A execução do degrau limite amplia a exposição protegida sem acionar a pausa.
- A [Proteção de posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) consome as execuções dos quatro blocos de entrada e usa distâncias absolutas de ganho e perda antes de enviar sua saída a mercado. O fechamento armazenado do candle finalizado só é liberado para uma verificação de preço enquanto o estado de prontidão está ativo. Um bloco [N valores](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) e variáveis de estado bloqueiam tanto entradas do primeiro degrau quanto essas verificações por exatamente 100 candles finalizados após uma execução do primeiro degrau a mercado ou uma saída protetora, restaurando ambas para o candle 101.
- O [Painel de gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles finalizados, EMA rápida 14, EMA lenta 50, os fluxos Order de limite de compra e venda e cinco fluxos MyTrade: compra a mercado, venda a mercado, compra limite, venda limite e saída protetora.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
