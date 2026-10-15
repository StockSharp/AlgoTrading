# Diagrama da estratégia Trade Report Alerts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama é um exemplo de geração de relatórios, e não de invenção de sinais. Um simples cruzamento de uma média móvel exponencial de 9 períodos com outra de 26 períodos em candles de cinco minutos finalizados fornece as operações, e tudo o que está ao redor transforma essas operações em texto legível: cada execução própria vira uma linha de log no instante em que acontece, e uma vez por dia um ramo comandado pelo relógio escreve o resultado realizado da estratégia. O lado dos relatórios lê o lado da negociação e nunca registra, altera ou bloqueia uma ordem própria.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados alimentam uma ExponentialMovingAverage rápida de 9 e outra lenta de 26. O bloco Crossing reduz o par a um único evento: `true` quando a linha rápida cruza acima da lenta, `false` quando cruza abaixo, e nada entre um e outro.
- A posição é lida uma vez por candle por meio de um instantâneo disparado pelo candle, e três comparações contra zero a descrevem como zerada, comprada ou vendida. Toda decisão é construída a partir desse instantâneo, de modo que uma execução que chegue no meio da barra não pode reabrir uma decisão já tomada.
- Com a posição zerada, um cruzamento para cima abre uma compra e um cruzamento para baixo abre uma venda. Os dois blocos de entrada carregam a condição Open-position, portanto ficam em silêncio enquanto houver qualquer posição mantida e não podem empilhar ordem sobre ordem.
- Com uma posição mantida, o cruzamento oposto a encerra por meio de um bloco Close-position, que dimensiona a ordem a partir da própria posição. O evento de ordem desse encerramento então dispara a entrada na nova direção, de modo que uma reversão é escrita como dois passos explícitos em vez de uma única ordem superdimensionada.
- Um único valor exposto de Volume alimenta os quatro blocos de entrada; os dois blocos de encerramento não recebem volume, porque um bloco Close-position já sabe quanto está aberto.
- O bloco Strategy trades capta cada execução própria da estratégia e a envia por um String formatter até uma notificação de Log, de modo que cada execução deixa uma linha com lado, quantidade, instrumento e preço.
- Um segundo ramo reporta pelo relógio, e não pelo mercado. Current time alimenta duas janelas Working time — uma janela de relatório em torno do meio-dia e uma janela de reinício logo depois da meia-noite — e um Flag colocado entre elas converte toda a janela de relatório em exatamente um pulso por dia.
- Esse único pulso libera o resultado realizado da estratégia a partir de uma variável que guarda o último valor recebido, o formata e o escreve no log. Como a variável começa em zero, uma linha de status aparece mesmo num dia que não produziu nenhuma execução.

## Regras de entrada e saída

- **Entrada comprada**: Um cruzamento para cima da média exponencial rápida sobre a lenta, avaliado enquanto o instantâneo tirado no tempo do candle mostra a posição zerada, envia uma compra a mercado por Volume. Se, em vez disso, houver uma posição vendida aberta, o mesmo cruzamento primeiro a encerra por completo, e a ordem de encerramento resultante dispara imediatamente a entrada comprada, de modo que a direção muda dentro do mesmo candle.
- **Entrada vendida**: Um cruzamento para baixo da média exponencial rápida abaixo da lenta, avaliado enquanto o instantâneo tirado no tempo do candle mostra a posição zerada, envia uma venda a mercado por Volume. Se, em vez disso, houver uma posição comprada aberta, o mesmo cruzamento primeiro a encerra por completo, e a ordem de encerramento resultante dispara imediatamente a entrada vendida.
- **Saída**: Não há bloco de stop, take-profit ou proteção: a posição é carregada até o cruzamento oposto, que a encerra por completo por meio de um bloco Close-position cujo volume é derivado da posição. O ramo dos relatórios observa execuções e lucro e nunca emite, substitui ou cancela uma ordem, de modo que desligar as notificações deixaria o comportamento de negociação inalterado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles; apenas candles finalizados movem os indicadores e todas as decisões construídas sobre eles. |
| Fast EMA Length | 9 | Período da ExponentialMovingAverage rápida; é a metade mais rápida do par de cruzamento. |
| Slow EMA Length | 26 | Período da ExponentialMovingAverage lenta; é a metade mais lenta do par de cruzamento. |
| Volume | 1 | Quantidade fornecida aos quatro blocos de entrada. Os dois blocos de encerramento a ignoram e tiram seu tamanho da posição aberta. |
| Report Window Begin | 12:00:00 | Início da janela diária de relatório. O primeiro instante dentro dela levanta o relatório de status. |
| Report Window End | 12:05:00 | Fim da janela diária de relatório. Ela só precisa ser larga o suficiente para que o relógio caia dentro dela uma vez; o flag mantém o relatório em uma única linha, qualquer que seja a largura. |
| Day Reset Begin | 00:00:00 | Início da janela de reinício que limpa o flag e permite um novo relatório no dia seguinte. |
| Day Reset End | 00:05:00 | Fim da janela de reinício. Entre esse momento e o início da janela de relatório o ramo permanece quieto. |
| Fill Report Caption | Trade report | Título escrito em cada notificação de execução, que é como as linhas por operação são reconhecidas no log. |
| Status Report Caption | Strategy status | Título escrito na notificação diária de status, o que a separa das linhas por operação. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite apenas candles de cinco minutos finalizados, e dois blocos [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calculam sobre eles valores de ExponentialMovingAverage de 9 e de 26. A filtragem por somente formados está desligada, então ambas as linhas ficam disponíveis desde o início da reprodução, e ambas são desenhadas no gráfico.
- O bloco [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) dispara apenas em um cruzamento; uma [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT converte seu evento de baixa em um gatilho positivo. A [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é armazenada em uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) liberada uma vez por candle, e três blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) a transformam em sinalizadores de posição zerada, comprada e vendida, que quatro condições AND combinam com o cruzamento.
- Seis blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) atuam sobre essas quatro condições: duas entradas a partir da posição zerada com a condição `OpenPosition`, dois encerramentos com a condição `ClosePosition` e mais duas entradas `OpenPosition` disparadas pelo evento de ordem do encerramento correspondente, que é o que faz a reversão acontecer em dois passos. Todos os seis enviam ordens a mercado e nenhum deles espera por uma conexão online.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) emite cada execução própria. Um [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) a renderiza com o modelo `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`, e uma [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) do tipo `Log` a escreve sob o título do relatório de execuções.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) alimenta duas verificações de [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html); a janela de relatório define um [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) e a janela de reinício o limpa, que é o que limita o ramo a um pulso por dia. O pulso libera o valor realizado do bloco [P&L](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) a partir de uma variável predefinida como zero, um segundo String Formatter escreve `Daily status: realized result {0}`, e uma segunda notificação `Log` o publica.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
