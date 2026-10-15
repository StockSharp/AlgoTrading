# Diagrama de estratégia de dois ativos com suas próprias médias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama alinha candles concluídos de quinze minutos de BTCUSDT@BNBFT e TONUSDT@BNBFT, compara cada fechamento com a média móvel simples de 20 períodos calculada para aquele instrumento e usa as relações opostas para gerenciar exposição comprada e vendida em BTCUSDT. Um latch com sinal atualizado por execuções, reversões a mercado em uma única etapa, saídas comuns e um stop fixo local de 2% completam o fluxo.

![schema](schema.svg)

## Visão geral da estratégia

- Variáveis separadas do tipo instrumento configuram somente as assinaturas de candles de BTCUSDT e TONUSDT. As ações de ordens e o fluxo Strategy trades usam o Strategy Security selecionado, que deve ser definido como BTCUSDT@BNBFT para coincidir com o parâmetro Traded Security.
- Somente candles concluídos de quinze minutos entram em um bloco Sync. Cada par alinhado fornece BTC Close e BTC SMA(20) por um ramo, e TON Close e TON SMA(20) pelo outro; a decisão começa apenas depois que as duas médias estão formadas.
- A relação comprada exige estritamente `BTC Close < BTC SMA(20)` junto com `TON Close > TON SMA(20)`. A relação vendida exige estritamente `BTC Close > BTC SMA(20)` junto com `TON Close < TON SMA(20)`. A igualdade não satisfaz nenhuma relação completa.
- Um latch numérico com sinal registra o estado de BTC gerenciado pelo diagrama: `-1` significa vendido, `0` zerado e `1` comprado. A partir do estado zerado, uma relação completa envia uma entrada a mercado de uma unidade. A partir do estado oposto, o volume da ação passa a duas unidades, fecha a exposição existente de uma unidade e estabelece uma unidade na nova direção com uma única ação a mercado.
- Uma relação oposta completa tem prioridade sobre uma saída comum. Nos demais casos, quando BTC está estritamente no lado de saída de sua própria média, a direção atual é fechada por uma ação ReduceOnly a mercado de uma unidade. Cada decisão sincronizada termina antes que o fechamento BTC armazenado seja entregue ao stop fixo local de 2% executado a mercado.

## Regras de entrada e saída

- **Entrada comprada**: Quando um par sincronizado de candles concluídos apresenta `BTC Close < BTC SMA(20)` e `TON Close > TON SMA(20)`, a porta de compra aceita o latch zerado ou vendido. Ela envia uma compra NoCondition a mercado com Volume 1 a partir do estado zerado, ou com Volume 2 a partir de uma posição vendida de uma unidade para reverter diretamente para uma posição comprada de uma unidade em BTC.
- **Entrada vendida**: Quando um par sincronizado de candles concluídos apresenta `BTC Close > BTC SMA(20)` e `TON Close < TON SMA(20)`, a porta de venda aceita o latch zerado ou comprado. Ela envia uma venda NoCondition a mercado com Volume 1 a partir do estado zerado, ou com Volume 2 a partir de uma posição comprada de uma unidade para reverter diretamente para uma posição vendida de uma unidade em BTC.
- **Saída**: Uma posição comprada é fechada com uma venda ReduceOnly a mercado de uma unidade quando BTC Close está estritamente acima de BTC SMA(20) e não há uma relação vendida completa. Uma posição vendida é fechada com uma compra ReduceOnly a mercado de uma unidade quando BTC Close está estritamente abaixo de BTC SMA(20) e não há uma relação comprada completa. Essas verificações de ausência reservam uma relação oposta completa para o ramo de reversão de duas unidades. A proteção local também pode fechar qualquer direção com um stop fixo de 2% a mercado; Take Profit 0 desativa o alvo de lucro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Instrumento usado somente pela assinatura de candles do ativo negociado. Defina Strategy Security com o mesmo valor BTCUSDT@BNBFT, pois todas as ações de ordens e o fluxo Strategy trades usam Strategy Security. |
| Signal Security | TONUSDT@BNBFT | Instrumento usado somente pela segunda assinatura de candles. Sua relação entre preço e média participa das decisões, mas nenhuma ação de ordem é endereçada a essa variável. |
| BTC Candles Series | 00:15:00 | Série de candles concluídos de quinze minutos de BTCUSDT usada para sincronização, BTC Close, BTC SMA(20), verificações de proteção e gráfico. |
| TON Candles Series | 00:15:00 | Série de candles concluídos de quinze minutos de TONUSDT usada para sincronização, TON Close, TON SMA(20) e gráfico. |
| BTC SMA Length | 20 | Período da SimpleMovingAverage calculada com candles BTCUSDT sincronizados e concluídos. |
| TON SMA Length | 20 | Período da SimpleMovingAverage calculada com candles TONUSDT sincronizados e concluídos. |
| Base Volume | 1 | Quantidade padrão de entrada a mercado. O volume da ação é `Base Volume * (1 + abs(latch))`, portanto uma entrada no estado zerado usa Base Volume e uma reversão usa o dobro de Base Volume; com o valor padrão, são Volume 1 e Volume 2. |
| Take Profit | 0 | Um valor absoluto igual a zero desativa a proteção de take-profit. |
| Stop Loss | 2% | Distância percentual adversa desde o preço da execução protegida que ativa o stop-loss. |
| Trailing Stop Loss | false | Desativado, portanto o stop de 2% permanece fixo em vez de acompanhar um movimento favorável do preço. |
| Use Market Orders | true | Ativado, portanto um stop disparado fecha a exposição protegida com uma ordem a mercado. |

## Detalhes do diagrama

- Dois blocos de [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) do tipo instrumento alimentam somente seus respectivos blocos de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Um bloco de [Sincronização](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/sync.html) alinha os fluxos concluídos em `00:15:00` antes que qualquer ramo alcance a cadeia de decisão.
- Cada candle sincronizado é dividido em Close e um valor formado de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). Quatro blocos de comparação estrita constroem as relações opostas comprada e vendida com o Close de cada instrumento e sua própria SMA(20).
- Uma variável Unit numérica mantém o latch com sinal. As execuções escrevem `1` depois de uma compra, `-1` depois de uma venda e `0` depois de uma saída comum ou protetora. Comparações de estado permitem entradas quando zerado e reversões a partir do lado oposto; a fórmula de volume é `Base Volume * (1 + abs(latch))`.
- As portas das relações comprada e vendida acionam blocos [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) configurados como NoCondition e MarketOrder. Ações ReduceOnly a mercado separadas tratam as saídas comuns. Cada porta de saída comum também exige que a relação oposta completa seja falsa, portanto uma reversão e um fechamento de uma unidade não podem ser solicitados para o mesmo par sincronizado.
- A [Proteção de posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) recebe execuções de entradas, reversões e saídas comuns. Take Profit é `0`, Stop Loss é `2%`, Trailing Stop Loss é `false`, Use Market Orders é `true`, e a proteção é executada localmente. O fechamento BTC sincronizado é armazenado primeiro e enviado à proteção somente depois que os dois ramos de sinais concluírem sua decisão para aquele par.
- O gráfico recebe os fluxos sincronizados de candles BTCUSDT e TONUSDT, BTC SMA(20), TON SMA(20), o fluxo de ordens stop-loss e todas as execuções BTCUSDT de Strategy trades.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
