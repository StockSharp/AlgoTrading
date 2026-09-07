# Diagrama da estratégia de rompimento da faixa da sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia rompimentos do BTCUSDT em uma faixa móvel de oito horas durante a sessão diurna UTC. Candles horários finalizados definem os níveis e as decisões, um único Flag diário libera o primeiro candidato direcional, e caminhos noturnos dedicados levam a posição criada pelo diagrama de volta a zero.

![schema](schema.svg)

## Visão geral da estratégia

- Cada candle horário finalizado é deslocado em um período antes de entrar nos indicadores Highest 8 e Lowest 8, configurados para emitir somente valores formados. A cada novo candle, os dois níveis representam as oito horas completas imediatamente anteriores e continuam se movendo, em vez de permanecerem fixos durante o dia.
- Um relógio Time comum ativa a redefinição diária das 00:00:00 às 07:59:59 UTC. O horário de abertura do candle controla separadamente a janela de negociação das 08:00:00 às 19:59:59 e a janela de fechamento das 20:00:00 às 23:59:59; essas configurações implementam os intervalos semiabertos `[08:00, 20:00)` e `[20:00, 24:00)`.
- Dentro da janela de negociação, a condição estrita `Close > High` com `Position <= 0` forma o candidato de compra, enquanto `Close < Low` com `Position >= 0` forma o candidato de venda. A igualdade com qualquer limite da faixa não aciona uma entrada.
- As duas direções compartilham um único Flag, portanto apenas o primeiro candidato de compra ou venda elegível pode entrar em cada dia UTC. A quantidade de entrada é `Base Volume + abs(Position)`: abre uma unidade a partir de zero ou fecha e reverte uma posição contrária de uma unidade com uma única ordem a mercado.
- Na janela de fechamento, uma posição positiva envia uma venda a mercado pelo volume base e uma posição negativa envia uma compra a mercado pelo mesmo volume. Não há blocos de stop-loss ou take-profit; o gráfico mostra candles, os níveis móveis Highest e Lowest e os quatro fluxos MyTrade.

## Regras de entrada e saída

- **Entrada comprada**: Das 08:00:00 às 19:59:59 UTC, quando um candle finalizado satisfaz `Close > Highest(8)` em relação às oito horas anteriores, a posição capturada é `<= 0` e o Flag diário compartilhado está disponível, o diagrama envia uma compra a mercado NoCondition de `1 + abs(Position)`.
- **Entrada vendida**: Das 08:00:00 às 19:59:59 UTC, quando um candle finalizado satisfaz `Close < Lowest(8)` em relação às oito horas anteriores, a posição capturada é `>= 0` e o Flag diário compartilhado está disponível, o diagrama envia uma venda a mercado NoCondition de `1 + abs(Position)`.
- **Saída**: Das 20:00:00 às 23:59:59 UTC, o diagrama vende Base Volume 1 quando a posição é positiva e compra Base Volume 1 quando ela é negativa. Na operação normal, as entradas criam exposição exatamente `+1` ou `-1`, por isso a quantidade fixa de saída a leva de volta a zero. Não há stop-loss, take-profit nem outra proteção conectada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Período de uma hora para BTCUSDT; apenas candles finalizados alimentam a faixa móvel, as verificações de sessão, as capturas de posição e as decisões. |
| Range Length | 8 | Número de candles finalizados e deslocados usados pelos indicadores Highest e Lowest com emissão somente de valores formados; o candle atual é excluído. |
| Reset Window | 00:00:00–07:59:59 UTC | Intervalo UTC em que o relógio Time comum redefine o Flag diário compartilhado antes da sessão de negociação. |
| Trade Window | 08:00:00–19:59:59 UTC | Limites UTC inclusivos configurados para candidatos de entrada, equivalentes ao intervalo semiaberto `[08:00, 20:00)` pelo horário de abertura do candle horário. |
| Close Window | 20:00:00–23:59:59 UTC | Limites UTC inclusivos configurados para zerar a posição, equivalentes ao intervalo semiaberto `[20:00, 24:00)` pelo horário de abertura do candle horário. |
| Base Volume | 1 | Quantidade unitária usada na entrada a partir de zero, somada a `abs(Position)` nas reversões e fornecida sem alteração às duas ordens noturnas de saída. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles horários finalizados de BTCUSDT. Um bloco [Valor anterior](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) com Shift 1 exclui o candle de decisão do cálculo da faixa.
- Dois blocos de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), configurados para emitir somente valores formados, calculam Highest 8 e Lowest 8 com o fluxo deslocado. Suas saídas são atualizadas a cada hora concluída e descrevem o canal móvel das oito horas anteriores.
- Um fluxo Time comum controla o bloco de redefinição [Horário de trabalho](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) para 00:00:00–07:59:59 UTC. O fluxo de candles controla diretamente os blocos dos horários de negociação e fechamento, de modo que essas decisões usam o OpenTime de cada candle.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é capturada para cada candle de decisão. Blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) e [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinam rompimento estrito, sessão, lado da posição e verificação do Flag compartilhado.
- A aritmética da quantidade calcula `Base Volume + abs(Position)`. Os dois blocos de entrada [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocam ordens a mercado NoCondition: abrem uma unidade comprada ou vendida a partir de zero ou revertem totalmente o lado oposto de uma unidade com uma só ordem.
- A janela de redefinição restaura um Flag compartilhado para o dia UTC, e o primeiro candidato de compra ou venda aceito o consome. Na janela de fechamento, caminhos separados para posições positivas e negativas enviam ordens a mercado com Base Volume 1 fixo e fecham a exposição `±1` produzida pelo caminho normal de entrada do diagrama.
- O [Painel do gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles finalizados, Highest 8, Lowest 8 e as saídas MyTrade da entrada comprada, entrada vendida, saída da posição comprada e saída da posição vendida. Não há elementos de stop-loss ou take-profit.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
