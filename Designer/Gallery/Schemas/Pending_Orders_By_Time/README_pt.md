# Diagrama de estratégia de rompimento virtual com ordens pendentes por horário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama usa candles finalizados de cinco minutos para armar, uma vez por dia, dois níveis virtuais simétricos de rompimento. O candle com horário 02:00 fornece o fechamento de referência; um toque posterior em qualquer nível registra uma única entrada a mercado, a proteção percentual gerencia a execução e o candle com horário 22:00 desarma a configuração e encerra qualquer posição restante. Os níveis virtuais são valores armazenados, não ordens em espera na bolsa.

![schema](schema.svg)

## Visão geral da estratégia

- Somente candles finalizados de cinco minutos controlam o horário, os cálculos de níveis, as verificações de rompimento e as atualizações do preço de proteção. A janela de abertura `02:00:00–02:04:59` seleciona exatamente o candle com horário 02:00, processado quando termina por volta das 02:05.
- Se não houver posição durante esse pulso de abertura, o diagrama armazena o fechamento do candle e calcula `Upper Level = Close × 1.0015` e `Lower Level = Close × 0.9985`. Os dois valores armazenados permanecem fixos e são substituídos no próximo pulso de abertura válido.
- Uma variável de estado armado e uma cadeia ordenada de acionadores de fim de candle garantem que High, Low e os dois níveis armazenados sejam atualizados antes da decisão. Um bloco Flag compartilhado e de uso único admite somente o primeiro rompimento da configuração diária.
- A comparação superior é avaliada antes da inferior. Se um candle atravessar os dois níveis, somente o rompimento superior será aceito e o diagrama registrará uma única compra a mercado; caso contrário, o rompimento inferior poderá registrar uma única venda a mercado. Nenhuma ordem existe antes que um nível seja tocado.
- As execuções de entrada inicializam a proteção de posição. Em seguida, os fechamentos dos candles finalizados controlam suas verificações de take-profit de 2% e stop-loss de 0.5%. A janela de fechamento `22:00:00–22:04:59` desarma qualquer configuração não utilizada e envia uma ação ReduceOnly a mercado limitada a Order Volume 1; sua execução retorna ao bloco de proteção para redefinir o acompanhamento da exposição.

## Regras de entrada e saída

- **Entrada comprada**: Enquanto o par virtual está armado, `High ≥ Upper Level` passa pelo gate diário de uso único e registra uma compra a mercado de Order Volume 1. O mesmo evento desativa os dois caminhos de rompimento até o próximo pulso de abertura válido.
- **Entrada vendida**: Se o rompimento superior não tiver sido aceito, uma condição armada `Low ≤ Lower Level` passa pelo gate de uso único e registra uma venda a mercado de Order Volume 1. Ela também desativa os dois caminhos de rompimento pelo restante do ciclo.
- **Saída**: A proteção de posição envia uma saída a mercado quando o fechamento de um candle finalizado atinge um movimento de 2% a favor do preço real de execução da entrada ou de 0.5% contra ele. De forma independente, o candle com horário 22:00 desarma o par virtual e solicita um fechamento ReduceOnly a mercado de, no máximo, Order Volume 1. Um limite de proteção intrabarra que não esteja presente no fechamento do candle finalizado não gera ação, e a configuração não é armada novamente após uma saída até a próxima janela de abertura.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles Series | 00:05:00 | Candles finalizados de cinco minutos usados para horário, níveis virtuais, testes de rompimento e verificações de proteção pelo preço de fechamento. |
| Opening Window | 02:00:00–02:04:59 | Intervalo inclusivo de um único candle que captura o fechamento de referência quando não há posição. |
| Closing Window | 22:00:00–22:04:59 | Intervalo inclusivo de um único candle que desarma uma configuração não utilizada e encerra uma posição aberta. |
| Entry Distance | 0.15% | Deslocamento percentual simétrico acima e abaixo do fechamento capturado. |
| Take Profit | 2% | Movimento favorável do preço de fechamento a partir do preço real de execução da entrada que aciona a proteção. |
| Stop Loss | 0.5% | Movimento adverso do preço de fechamento a partir do preço real de execução da entrada que aciona a proteção. |
| Order Volume | 1 | Quantidade enviada por qualquer uma das entradas a mercado e redução máxima programada da posição. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite candles finalizados de cinco minutos. Blocos [Conversor](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/converter.html) para Close, High e Low fornecem fluxos numéricos explícitos.
- Dois blocos [Horário de trabalho](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) inspecionam OpenTime do candle. Seus limites superiores terminam um segundo antes da próxima marca de cinco minutos porque os dois limites configurados são inclusivos.
- Blocos [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) armazenam o fechamento de referência, os níveis calculados, o estado armado, o lado escolhido e as constantes. Dois blocos [Fórmula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) calculam os deslocamentos percentuais simétricos somente durante um pulso de abertura válido.
- Blocos [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) e [Flag](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/flag.html) impõem o arme somente sem posição, a avaliação com valores atualizados, a precedência da compra e apenas um rompimento aceito por configuração.
- Os blocos de compra e venda [Registro de ordens](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/orders/register.html) são ações de ordens a mercado. Suas saídas MyTrade inicializam o bloco compartilhado [Proteção de posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), cuja entrada Price recebe os fechamentos dos candles finalizados.
- O bloco programado [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) usa ReduceOnly com Order Volume 1. Ele deriva a direção de fechamento da exposição atual, nunca aumenta a posição e retorna sua saída MyTrade para a proteção de posição após uma saída programada.
- O [Painel de gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) mostra os candles finalizados, os dois níveis virtuais armazenados, as ordens de entrada e proteção e as execuções de entrada, proteção e fechamento programado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
