# Diagrama da estratégia com sinal entre instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama sincroniza candles concluídos de quatro horas de TONUSDT@BNBFT e BTCUSDT@BNBFT. TONUSDT fornece um sinal de taxa de variação de 20 períodos, enquanto BTCUSDT fornece seu próprio filtro de média móvel simples de 20 períodos. O diagrama foi preparado para Strategy Security BTCUSDT@BNBFT; um latch numérico `0/1` zerado/comprado, um estado compartilhado de permissão de entrada e proteção orientada por execuções administram essa exposição somente comprada.

![schema](schema.svg)

## Visão geral da estratégia

- Duas variáveis de instrumento independentes configuram somente as assinaturas de candles de quatro horas de TONUSDT e BTCUSDT. Os candles concluídos são alinhados antes da decisão, portanto o impulso TON e o filtro de tendência BTC sempre pertencem ao mesmo intervalo sincronizado.
- TON ROC(20) é positivo acima de zero e se torna condição de saída em zero ou abaixo. BTCUSDT permite a entrada quando seu Close está igual ou acima de SMA(20), e um Close abaixo de SMA(20) é condição de saída.
- A entrada comprada exige quatro condições simultâneas: `TON ROC(20) > 0`, `BTC Close >= BTC SMA(20)`, o latch interno indica zerado e o estado compartilhado `Cooldown is ready` permite a entrada. O AND montado externamente aciona então uma compra NoCondition a mercado com volume 1.
- A ação de entrada retira a permissão compartilhada e inicia Entry Cooldown N, enquanto a venda discricionária por sinal retira a mesma permissão e inicia Signal-exit Cooldown N. O temporizador correspondente restaura a permissão depois de oito pares de candles sincronizados; uma verificação de geração suprime a conclusão de um temporizador anterior após uma reinicialização mais recente. As saídas não aguardam esse estado, e saídas de proteção não o reiniciam.
- A execução da compra a mercado de BTCUSDT muda o latch para comprado e ativa take-profit de 2% e stop-loss fixo, não móvel, de 2.5%. Uma execução do fechamento discricionário ou a ativação e execução de Take/Stop devolve o latch ao estado zerado. A estratégia nunca abre posição vendida.

## Regras de entrada e saída

- **Entrada comprada**: Em um par sincronizado de candles concluídos de quatro horas, o AND externo de entrada aciona uma compra NoCondition a mercado com volume 1 quando TON ROC(20) está acima de zero, BTC Close está igual ou acima de BTC SMA(20), o latch indica zerado e o estado compartilhado `Cooldown is ready` permite a entrada. A ação negocia o Strategy Security selecionado, que deve ser BTCUSDT@BNBFT para coincidir com o parâmetro de candles Traded Security.
- **Entrada vendida**: Não há entrada vendida. A venda discricionária usa ReduceOnly, MarketOrder e volume 1, portanto só pode reduzir a exposição do Strategy Security selecionado; as proteções Take e Stop também fecham a exposição comprada.
- **Saída**: Quando o latch indica comprado, `TON ROC(20) <= 0` ou `BTC Close < BTC SMA(20)` aciona a venda ReduceOnly a mercado sem aguardar o estado compartilhado de intervalo. O take-profit de 2% ou o stop-loss fixo de 2.5% também pode fechar a exposição com ordem a mercado; o stop não acompanha o preço.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Instrumento usado somente pela assinatura de candles do ativo negociado. O Strategy Security selecionado deve ter o mesmo valor BTCUSDT@BNBFT, pois ações e negócios da estratégia usam Strategy Security. |
| Signal Security | TONUSDT@BNBFT | Instrumento usado somente pela assinatura de candles de sinal; seu impulso contribui ao sinal e nenhuma ação é endereçada a essa variável. |
| BTC Candles Series | 04:00:00 | Série de candles concluídos de quatro horas de BTCUSDT usada para Close, SMA(20), decisões de estado e gráfico. |
| TON Candles Series | 04:00:00 | Série de candles concluídos de quatro horas de TONUSDT usada para ROC(20) e decisões sincronizadas. |
| BTC SMA Length | 20 | Período da SimpleMovingAverage calculada com candles concluídos de BTCUSDT. |
| TON ROC Length | 20 | Período da RateOfChange calculada com candles concluídos de TONUSDT. |
| ROC Threshold | 0 | Nível zero que separa o estado de impulso positivo para entrada do sinal não positivo para saída. |
| Entry Cooldown N | 8 | Número de pares de candles sincronizados contados pelo temporizador da ação de entrada antes que ele possa restaurar a permissão compartilhada. |
| Signal-exit Cooldown N | 8 | Número de pares de candles sincronizados contados pelo temporizador da saída discricionária por sinal antes que ele possa restaurar a permissão compartilhada. |
| Order Volume | 1 | Quantidade fixa usada pela compra NoCondition a mercado e pela venda ReduceOnly a mercado do Strategy Security selecionado. |
| Take Profit | 2% | Ganho percentual desde o preço executado da entrada que ativa o take-profit. |
| Stop Loss | 2.5% | Perda percentual desde o preço executado da entrada que ativa o stop-loss. |
| Trailing Stop Loss | false | Desativado, portanto o stop-loss de 2.5% permanece fixo e não acompanha um movimento favorável do preço. |
| Use Market Orders | true | Ativado, portanto take-profit e stop-loss fecham a posição com ordens a mercado. |

## Detalhes do diagrama

- Blocos separados de [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) do tipo instrumento alimentam somente as assinaturas independentes de candles TONUSDT@BNBFT e BTCUSDT@BNBFT. As ações de ordem e os Negócios da estratégia usam Strategy Security; selecione também ali BTCUSDT@BNBFT para coincidir com o parâmetro de candles Traded Security.
- Dois blocos de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emitem somente candles concluídos de quatro horas. Um bloco [Sync](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/sync.html) emparelha os dois fluxos no intervalo `04:00:00` antes que qualquer instrumento entre na cadeia de decisão.
- Um bloco de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula RateOfChange 20 para TONUSDT e outro calcula SimpleMovingAverage 20 para BTCUSDT. Blocos de comparação expressam os estados positivo e não positivo do ROC e as relações de BTC Close acima ou abaixo de sua média.
- Uma [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Unit numérica funciona como latch de estado: `0` significa zerado e `1` comprado. Buy MyTrade escreve `1`; o MyTrade da venda discricionária e os eventos de ativação e MyTrade de Take/Stop escrevem `0`. Blocos lógicos combinam esse estado com os sinais sincronizados.
- Dois temporizadores N valores específicos por ação mantêm Entry Cooldown N e Signal-exit Cooldown N em 8. Qualquer uma das ações retira uma única permissão compartilhada de entrada; seu temporizador pode restaurar `Cooldown is ready` depois de oito pares sincronizados, enquanto uma verificação de geração rejeita a conclusão obsoleta de um temporizador anterior. O AND externo de entrada zerada tem uma única entrada de intervalo. Ele aciona uma compra [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) configurada como NoCondition, MarketOrder e volume 1; a saída por sinal aciona uma venda separada ReduceOnly, MarketOrder e volume 1 sem essa entrada.
- A [Proteção de posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) recebe as execuções da compra e do fechamento discricionário: a compra ativa Take Profit `2%` e Stop Loss fixo `2.5%`, enquanto o fechamento limpa o estado de proteção obsoleto. Trailing Stop Loss é `false` e Use Market Orders é `true`. O gráfico recebe os dois fluxos de candles sincronizados, BTC SMA(20), TON ROC(20), os fluxos de ordens Take e Stop e todas as execuções BTC dos Negócios da estratégia.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
