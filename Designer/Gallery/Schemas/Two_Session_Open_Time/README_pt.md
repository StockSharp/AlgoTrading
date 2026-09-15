# Diagrama da estratégia Two Session Open Time
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama não contém nenhum indicador: o relógio é a única fonte de sinais. Duas janelas distintas do pregão abrem, cada uma, uma posição comprada, um Flag limita cada janela a uma única entrada por dia, e uma terceira janela zera o que ainda estiver aberto e rearma as duas janelas para o dia seguinte.

![schema](schema.svg)

## Visão geral da estratégia

- Time envia o momento atual para três blocos Working time: duas janelas de entrada, 09:30-14:00 e 00:00-04:00, e uma janela de fechamento forçado, 19:50-20:00.
- Cada janela de entrada é combinada com a verificação de posição zerada por um Logical condition ajustado em And, de modo que a janela só pode pedir uma entrada enquanto nada estiver aberto.
- Uma janela fica aberta por horas e seu gate repete o mesmo valor verdadeiro. O Flag fica entre o gate e a ordem e deixa passar apenas o primeiro deles, o que transforma uma janela longa em uma única entrada.
- As duas janelas compram. O Position modify trabalha com a condição Open position, portanto uma ordem a mercado de Order Volume só sai quando a posição está exatamente em zero.
- A janela de fechamento forçado aciona um terceiro Position modify configurado em Close position, e esse mesmo sinal reinicia os dois Flags, de modo que as duas janelas de entrada ficam armadas de novo para o dia seguinte.
- O Position protection acompanha as execuções das duas entradas e fecha a posição em um take-profit de 1.5% ou em um stop móvel de 0.5% que segue o fechamento do candle.
- Candles finalizados de cinco minutos ditam o ritmo de todo o diagrama: são eles que levam o preço de fechamento ao Position protection, são eles que o painel desenha, e sua chegada é o que faz o relógio avançar.
- O painel de gráfico mostra os candles, a linha de preço à qual a proteção reage, cada ordem que o diagrama envia e cada execução que ele recebe.

## Regras de entrada e saída

- **Entrada comprada**: Dentro de qualquer uma das janelas, enquanto a posição está zerada, o Flag daquela janela libera seu primeiro sinal verdadeiro e o Position modify compra Order Volume a mercado sob a condição Open position. Todo sinal posterior da mesma janela é absorvido pelo Flag até que a janela de fechamento o reinicie.
- **Entrada vendida**: Não há lado vendido. As duas janelas abrem posições compradas, e as únicas ordens de venda que o diagrama chega a enviar são as que fecham uma posição comprada aberta.
- **Saída**: O Position protection fecha a posição em um take-profit de 1.5% ou em um stop móvel de 0.5% que segue o fechamento do candle. Tudo o que ainda estiver aberto quando a janela de fechamento começa é zerado pela ação Close position, que também limpa as duas travas.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame de cinco minutos; apenas candles finalizados são processados, e seus fechamentos são a base do preço verificado pela proteção e da linha do gráfico. |
| First Window From | 09:30:00 | Início da primeira janela de entrada no horário da reprodução ou do servidor. |
| First Window Until | 14:00:00 | Fim da primeira janela de entrada; depois dele essa janela não pode mais armar uma entrada. |
| Second Window From | 00:00:00 | Início da segunda janela de entrada no horário da reprodução ou do servidor. |
| Second Window Until | 04:00:00 | Fim da segunda janela de entrada. |
| Close Window From | 19:50:00 | Início da janela de fechamento forçado, que zera uma posição aberta e reinicia as duas travas. |
| Close Window Until | 20:00:00 | Fim da janela de fechamento forçado. |
| Order Volume | 1 | Quantidade fixa usada pelas entradas das duas janelas. |
| Take Profit, % | 1.5 | Movimento percentual favorável no qual o Position protection fecha a posição. |
| Stop Loss, % | 0.5 | Movimento percentual adverso do stop; a técnica de trailing o desloca atrás do fechamento do candle assim que o preço corre a favor. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles finalizados de cinco minutos. Um [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) toma o preço de fechamento deles, que é o preço contra o qual o [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) mede seu take-profit e seu stop móvel, e também a linha desenhada junto aos candles. Nada mais é calculado a partir do preço: o diagrama não tem nenhum indicador.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) fornece o momento atual a três blocos [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html). Dois deles marcam as janelas de entrada e um marca a janela de fechamento forçado; a reprodução do histórico incluído roda em UTC, portanto os limites das janelas são lidos como horários UTC.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) comparado com uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) igual a zero por meio de [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) produz a verificação de posição zerada que os dois gates And de [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) compartilham, de modo que uma posição aberta bloqueia silenciosamente também a outra janela.
- [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) é o que transforma uma janela em um evento único. Seu gatilho é o gate And e seu reset é a janela de fechamento; ele passa um valor apenas no instante em que é acionado, de modo que as centenas de leituras verdadeiras produzidas por uma janela de quatro horas se reduzem a uma única entrada.
- Três blocos [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) atuam: duas entradas Open position que tomam Order Volume e um fechamento Close position que não precisa de volume, porque lê a posição que tem de desfazer. As execuções das duas entradas são unidas por [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) e entregues ao Position protection, cuja própria execução de fechamento é desenhada no painel, mas não é realimentada em sua entrada de negócios.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
