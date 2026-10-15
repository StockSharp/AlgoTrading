# Diagrama da estratégia Open Drive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama toma um único candle de impulso: aquele cujo corpo é maior do que uma fração do Average True Range atual. A cor desse corpo decide o lado, a SMA 20 tem de concordar com ela, o relógio tem de estar dentro das primeiras seis horas do dia UTC e a posição tem de estar zerada. Um take profit e um stop loss são a única saída.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados fornecem Close e Open através de dois Converters e alimentam a SMA 20 e o ATR 14. Ambos os indicadores entregam apenas valores formados, portanto nenhuma comparação produz um veredito enquanto cada um não tiver reunido candles suficientes.
- Uma Formula mede o corpo do candle atual como `abs(Close - Open)`; uma segunda transforma o ATR atual em um limiar, `ATR x 0.3`. Uma Comparison classifica o candle como impulso quando o corpo é estritamente maior do que esse limiar, de modo que a barra sobre a qual o diagrama age é anormalmente grande para a volatilidade do momento.
- Duas Comparisons leem a cor do mesmo candle, `Close > Open` e `Close < Open`, e outras duas leem o seu lado em relação à SMA 20, `Close > SMA` e `Close < SMA`. O impulso sozinho nunca negocia: cor e tendência têm de apontar para o mesmo lado.
- O bloco Current time envia o tempo da estratégia para um bloco Working time que cobre de 00:00:00 a 06:00:00 UTC. Sua resposta verdadeiro/falso fica guardada em uma Variable que a republica quando chega um candle, de modo que o filtro de sessão é decidido no mesmo tick que todas as comparações de preço, e não pelo seu próprio relógio.
- Uma Variable tira um snapshot da posição a cada candle e uma Comparison contra zero informa se o diagrama está zerado. Ler a posição por meio de um snapshot impede que uma execução ocorrida entre dois candles reabra a lógica de entrada no meio da barra.
- A Logical condition de compra é `impulso E corpo de alta E fechamento acima da SMA E dentro da janela E posição zerada`; a de venda é o seu espelho. Cada uma espera por todas as cinco entradas, portanto publica exatamente um veredito por candle finalizado.
- Um veredito verdadeiro aciona um bloco Modify position no modo OpenPosition, que envia uma ordem a mercado com o volume configurado e a recusa a menos que a posição esteja realmente zerada. Um mesmo candle nunca pode, assim, abrir duas operações, e uma posição aberta bloqueia por completo novas entradas.
- As execuções de entrada dos dois lados passam por uma Combination até a Position protection, que arma um take profit de 3% e um stop loss de 2% contra o preço de fechamento de cada candle finalizado posterior.

## Regras de entrada e saída

- **Entrada comprada**: Dentro de 00:00:00-06:00:00 UTC, com SMA 20 e ATR 14 formados e a posição zerada: `abs(Close - Open) > ATR x 0.3`, `Close > Open` e `Close > SMA 20` enviam uma compra a mercado OpenPosition de uma unidade.
- **Entrada vendida**: Dentro de 00:00:00-06:00:00 UTC, com SMA 20 e ATR 14 formados e a posição zerada: `abs(Close - Open) > ATR x 0.3`, `Close < Open` e `Close < SMA 20` enviam uma venda a mercado OpenPosition de uma unidade.
- **Saída**: Não há saída por sinal nem inversão. A Position protection encerra a operação com 3% de lucro ou 2% de prejuízo em relação ao preço de execução da entrada, avaliados no fechamento de cada candle finalizado, de modo que um pico intrabar que atravesse um nível só é considerado quando aquele candle termina. O diagrama não mantém nenhum contador de espera entre operações: assim que uma posição é encerrada, o próximo candle qualificado dentro da janela já pode abrir outra.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame dos candles finalizados; todas as comparações, ambos os indicadores e os níveis de proteção são avaliados nos seus fechamentos. |
| MA Period | 20 | Período da média móvel simples, usada apenas quando formada, que decide de que lado da tendência o candle fechou. |
| ATR Period | 14 | Período do Average True Range, usado apenas quando formado, que descreve o tamanho normal do candle naquele momento. |
| ATR Multiplier | 0.3 | Fração do ATR atual que o corpo de um candle precisa superar para ser considerado impulso. Aumentá-la exige candles mais raros e maiores; reduzi-la aceita candles comuns. |
| Window Begin | 00:00:00 | Início da janela de negociação, em UTC. Antes dele, os impulsos são medidos e desenhados, mas nunca negociados. |
| Window End | 06:00:00 | Fim da janela de negociação, em UTC. Amplie o par para 00:00:00-23:59:59 para deixar o diagrama negociar 24 horas por dia. |
| Order Volume | 1 | Quantidade enviada por ambas as entradas; a posição é sempre de uma unidade, porque uma segunda entrada é recusada enquanto ela estiver aberta. |
| Take Profit | 3% | Distância do take profit, como percentual do preço de execução da entrada. |
| Stop Loss | 2% | Distância do stop loss, como percentual do preço de execução da entrada. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles de cinco minutos finalizados, que o histórico de minutos incluído consegue montar. Dois [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) leem Close e Open, e dois blocos [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), que entregam apenas valores formados, calculam a SMA 20 e o ATR 14.
- Dois blocos [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) constroem o corpo e o limiar do ATR, e cinco blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) transformam corpo, cor, lado da tendência e posição em sinais.
- O bloco [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) alimenta o tempo da estratégia no [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html), cuja resposta muda muito mais vezes do que um candle. Uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) com Input as trigger desligado guarda essa resposta e só a libera quando o próximo candle a aciona.
- A [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) recebe um snapshot de uma segunda Variable e é comparada com uma constante zero. Ambos os blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) de cinco entradas esperam por todas elas, de modo que cada candle produz um veredito de compra e um de venda.
- Dois blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) em OpenPosition negociam a mercado. Uma [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) reúne as execuções de entrada dos dois lados para a [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), e o [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) desenha candles, SMA, ATR, todas as ordens, incluindo o par de proteção, e todas as execuções.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
