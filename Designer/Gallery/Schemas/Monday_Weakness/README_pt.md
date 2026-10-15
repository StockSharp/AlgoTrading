# Diagrama da estratégia Monday Weakness
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama opera um calendário semanal fixo. Cada lado da semana tem o seu dia: no dia de venda ele vende quando o fechamento fica abaixo da SMA 20, e no dia de recompra ele compra essa posição vendida de volta; no dia de compra ele compra quando o fechamento fica acima da SMA 20, e no dia de saída ele vende essa posição comprada. O dia da semana é lido diretamente do candle como um número, de modo que as quatro regras de calendário são quatro comparações comuns, e uma janela de entrada guiada pelo relógio mantém a decisão semanal dentro da parte ativa do dia.

![schema](schema.svg)

## Visão geral da estratégia

- Os candles de cinco minutos são entregues apenas finalizados. Dois blocos Converter leem o mesmo candle: um pega o preço de fechamento, o outro pega o dia da semana do horário de abertura, que chega como um número em que domingo é 0 e sábado é 6.
- Quatro blocos Variable guardam os quatro dias do calendário — venda, recompra, compra e saída — e quatro blocos Comparison ajustados para Equal transformam o número do dia em quatro sinais. Exatamente um deles pode ser verdadeiro em um mesmo candle, e é isso que impede que os quatro ramos concorram entre si.
- A SMA 20 roda sobre os mesmos candles e não publica nada até estar formada, portanto os primeiros vinte candles da execução não produzem sinal algum. Dois blocos Comparison a leem: um é verdadeiro enquanto o fechamento está abaixo da média, o outro enquanto está acima dela.
- Um bloco Position, uma Variable com o valor zero e um Comparison ajustado para Equal formam a verificação de posição zerada. Os dois ramos de entrada a exigem, de modo que uma semana que já está no mercado não pode empilhar uma segunda posição sobre a primeira.
- O Current time alimenta o Working time, que é verdadeiro entre 08:00:00 e 20:00:00. Os dois ramos de entrada também exigem essa janela, de modo que uma posição semanal nunca é aberta em um candle noturno de baixa liquidez. As duas saídas ficam deliberadamente fora da janela — o que estiver aberto deve ser encerrado no seu dia de calendário, a qualquer hora em que o sinal apareça.
- Dois blocos Logical condition AND reúnem as entradas. O ramo vendido precisa do dia de venda, de um fechamento abaixo da SMA 20, da posição zerada e da janela aberta; o ramo comprado precisa do dia de compra, de um fechamento acima da SMA 20 e dos mesmos dois filtros. Cada um aciona um bloco Modify position com a condição Open position, de modo que um sinal repetido dentro do mesmo dia não consegue enviar uma segunda ordem.
- As duas saídas são blocos Modify position com a condição Close position e um lado explícito. O bloco do dia de recompra é uma compra, portanto só pode encerrar uma posição vendida; o bloco do dia de saída é uma venda, portanto só pode encerrar uma posição comprada. Nenhum deles recebe entrada de volume, porque o Close position dimensiona a ordem a partir da própria posição aberta.
- O Chart panel desenha a série de candles, a SMA 20, as ordens das quatro ações e as execuções que elas produzem, de modo que o ritmo semanal — entrada no início da semana, recompra no meio da semana, entrada no fim da semana, saída no encerramento — pode ser lido diretamente da imagem.

## Regras de entrada e saída

- **Entrada comprada**: No dia de compra, dentro da janela de entrada, com a posição zerada e o fechamento acima da SMA 20, uma compra a mercado de Order volume é enviada através do Modify position com a condição Open position.
- **Entrada vendida**: No dia de venda, dentro da janela de entrada, com a posição zerada e o fechamento abaixo da SMA 20, uma venda a mercado de Order volume é enviada através do Modify position com a condição Open position.
- **Saída**: As saídas são por calendário, não por preço. No dia de recompra, um Modify position de compra com a condição Close position zera uma posição vendida aberta; no dia de saída, um Modify position de venda com a mesma condição zera uma posição comprada aberta. Não há stop, nem alvo, nem regra de trailing, de modo que uma posição é carregada até chegar o seu próprio dia de saída. Os dois sinais de saída se repetem em cada candle do seu dia, e nada conta nem suprime essa repetição: o primeiro candle encerra a posição e, a partir daí, o Close position não tem com o que trabalhar e rejeita o sinal silenciosamente. O mesmo vale para as entradas — não há contador diário nem intervalo de espera entre as operações, e é a condição Open position junto com a verificação de posição zerada que mantém um dia de calendário limitado a uma única ordem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles. Apenas candles finalizados são entregues; o dia da semana, a média e todos os sinais são lidos de candles concluídos. |
| MA Period | 20 | Período da média móvel simples que filtra as duas entradas. A média só publica valores depois de formada, portanto nenhuma entrada é possível durante os primeiros candles de uma execução. |
| Session From | 08:00:00 | Início da janela diária em que as entradas são permitidas, lido do relógio da estratégia. As saídas ignoram essa janela. |
| Session Until | 20:00:00 | Fim dessa janela. Alargue o par para deixar a regra de calendário agir a qualquer hora, estreite-o para concentrar as entradas em algumas horas do dia. |
| Short day | 1 | Número do dia que abre uma posição vendida quando o fechamento está abaixo da média. Os dias são numerados de domingo como 0 até sábado como 6. |
| Cover day | 3 | Número do dia em que uma posição vendida aberta é recomprada. Ele encerra apenas uma posição vendida; nesse dia uma posição comprada fica intocada. |
| Long day | 4 | Número do dia que abre uma posição comprada quando o fechamento está acima da média. Numerado na mesma escala, com domingo como 0. |
| Exit day | 5 | Número do dia em que uma posição comprada aberta é vendida. Ele encerra apenas uma posição comprada; nesse dia uma posição vendida fica intocada. |
| Order volume | 1 | Quantidade fixa usada pelas duas ações Open position. As duas ações Close position não precisam de volume, porque dimensionam a ordem a partir do que estiver aberto. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está configurado apenas para candles finalizados, o que importa duas vezes: o dia da semana é tirado de um candle que não vai mais mudar, e toda ordem que o diagrama envia leva o horário de fechamento de um candle concluído, e não o horário de abertura de um que ainda está se formando.
- Tanto o calendário quanto o preço vêm de um par de [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) sobre essa mesma série. Ler o dia da semana do candle, e não de um relógio separado, mantém o teste de calendário exatamente no mesmo compasso do teste de tendência, de modo que os dois sempre descrevem o mesmo candle quando os blocos [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND os reúnem.
- Os quatro números de dia são [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) comuns comparadas por blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), e é por isso que todo o plano semanal pode ser rearranjado a partir da lista de parâmetros: mude o dia de venda para outro número e o esquema passa a operar nesse dia, sem tocar em nenhuma ligação.
- O [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) e o [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) fornecem o filtro de hora do dia como um nível simples: verdadeiro durante toda a janela, falso fora dela. Ele está ligado apenas aos dois ramos de entrada, e o instantâneo de [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) ao lado fornece a verificação de posição zerada da mesma forma.
- Quatro blocos [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) fazem toda a negociação: dois com Open position e volume fixo, dois com Close position e lado declarado. Dar um lado aos blocos de encerramento é o que torna as saídas de calendário exatas — uma compra no dia de recompra simplesmente recusa uma posição comprada, e uma venda no dia de saída simplesmente recusa uma posição vendida. Tudo o que eles emitem é desenhado no [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto com os candles e a SMA 20.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
