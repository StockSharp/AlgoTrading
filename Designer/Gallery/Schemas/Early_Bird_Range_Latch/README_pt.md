# Diagrama da estratégia Early Bird Range Latch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia um rompimento estrito do extremo da vela anterior de cinco minutos quando o preço concorda com a direção da EMA 20. Uma trava diária UTC aceita no máximo uma nova posição por dia, enquanto o ATR 14 atual define os limites de stop e alvo.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de cinco minutos fornecem o fechamento atual, a máxima e a mínima da vela anterior, a EMA 20 e o ATR 14. Os blocos Previous value deslocam apenas os fluxos High e Low, por isso a decisão nunca compara uma vela com os próprios extremos.
- A configuração comprada exige `Close > previous High` e `Close > EMA 20`; a vendida exige `Close < previous Low` e `Close < EMA 20`. Todas as comparações são estritas, portanto igualdade não gera sinal.
- Um bloco Time controla o intervalo fixo de reinício diário de 00:00:00 a 00:04:59 UTC. O horário da vela controla o intervalo fixo de entrada de 00:05:00 a 23:59:59, e um Flag compartilhado libera apenas a primeira configuração direcional válida depois de cada reinício.
- Uma configuração aceita guarda o fechamento atual como preço de entrada e abre uma unidade a mercado somente quando o retrato da posição é zero. A trava continua consumida depois da saída e impede nova entrada até o próximo reinício diário UTC.
- Em cada vela concluída posterior, as fórmulas recalculam quatro limites usando a entrada guardada e o ATR atual: stop e alvo comprados em `entry − 1.5×ATR` e `entry + 2.5×ATR`, com os sinais invertidos para uma venda. Ações ReduceOnly a mercado fecham o lado correspondente quando qualquer limite é atingido.

## Regras de entrada e saída

- **Entrada comprada**: Depois de formada a EMA 20, entre 00:05:00 e 23:59:59 UTC, posição zerada, `Close > previous High`, `Close > EMA 20` e Flag diário disponível enviam uma compra OpenPosition a mercado de uma unidade.
- **Entrada vendida**: Depois de formada a EMA 20, entre 00:05:00 e 23:59:59 UTC, posição zerada, `Close < previous Low`, `Close < EMA 20` e Flag diário disponível enviam uma venda OpenPosition a mercado de uma unidade.
- **Saída**: Para uma posição comprada, uma venda ReduceOnly a mercado dispara em `Close ≤ entry − 1.5×current ATR` ou `Close ≥ entry + 2.5×current ATR`. Para uma posição vendida, uma compra ReduceOnly a mercado dispara em `Close ≥ entry + 1.5×current ATR` ou `Close ≤ entry − 2.5×current ATR`. Não há saída por horário, trailing, reversão ou reentrada no mesmo dia.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período das velas concluídas que acionam todos os cálculos de sinal e risco. |
| EMA Length | 20 | Período da média móvel exponencial formada usada como filtro direcional. |
| ATR Length | 14 | Período do Average True Range formado e recalculado para cada vela concluída. |
| Stop ATR Multiplier | 1.5 | Multiplicador aplicado ao ATR atual para colocar o limite adverso em relação ao preço de entrada guardado. |
| Target ATR Multiplier | 2.5 | Multiplicador aplicado ao ATR atual para colocar o limite favorável em relação ao preço de entrada guardado. |
| Order Volume | 1 | Quantidade fixa fornecida às duas entradas OpenPosition e às duas saídas ReduceOnly. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas concluídas de cinco minutos e pode construí-las a partir do histórico por minuto incluído.
- Três [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extraem Close, High e Low. Dois blocos [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) aplicam Shift 1 aos fluxos numéricos High e Low.
- Blocos [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) com saída apenas depois de formados calculam EMA 20 para direção e ATR 14 para distância de risco. A prontidão da EMA também evita entradas antes de ambos os indicadores acumularem dados suficientes.
- O fluxo [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) alimenta o bloco de reinício [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html). Outro bloco Working time lê o horário da vela e participa diretamente das duas condições de entrada.
- Um [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) compartilhado consome o primeiro candidato comprado ou vendido do dia UTC. Blocos Variable capturam a posição e o fechamento da entrada aceita; uma segunda variável de preço de entrada republica o valor guardado em cada vela para as fórmulas de risco.
- Blocos [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) e [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) montam os filtros de rompimento e os quatro limites de ATR. Flags de saída por vela impedem fechamentos duplicados quando várias entradas atualizam durante uma avaliação.
- Dois blocos OpenPosition e dois ReduceOnly de [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) executam entradas e saídas a mercado. O [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe velas, High e Low anteriores, EMA, ATR e um fluxo Combination com todas as execuções.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
