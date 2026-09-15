# Diagrama da estratégia Delayed Reversal Session
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama coloca duas coisas entre um sinal e uma ordem: uma espera e um relógio. Um corredor percentual é traçado em torno de uma média móvel, e a saída do preço desse corredor é tratada como uma reversão. A reversão não é negociada no momento em que acontece. Ela é entregue a um bloco de atraso que conta um número fixo de candles finalizados, e apenas o impulso que sai do outro lado tem permissão de chegar perto dos blocos de ordem — e somente enquanto o bloco de horário de funcionamento indicar que o relógio está dentro da sessão.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles, de trinta minutos, somente candles finalizados, alimenta todos os ramos do diagrama.
- Uma média móvel fornece a linha central; uma constante guarda a sensibilidade, e duas fórmulas transformam o par em uma borda superior e uma borda inferior — a média mais e menos a média vezes a sensibilidade.
- Dois blocos de cruzamento acompanham o fechamento em relação a essas bordas. Um dispara quando o fechamento cruza a borda superior de baixo para cima; o outro está ligado ao contrário, com a borda inferior na entrada de alta, de modo que dispara quando o fechamento cai através da borda inferior.
- Cada cruzamento arma o seu próprio bloco de atraso. O atraso conta os candles finalizados que chegam depois do armamento e libera um único impulso quando a contagem se esgota, de modo que o sinal é executado mais tarde, e não no candle que o produziu.
- O bloco de horário de funcionamento lê o horário de cada candle e informa se o momento está dentro da sessão; um NOT lógico transforma esse mesmo sinalizador em um sinalizador de fora da sessão.
- Um AND lógico por lado une o impulso liberado ao sinalizador de sessão, de modo que um impulso que caia fora do horário de funcionamento é descartado em vez de ficar na fila.
- As entradas são ordens a mercado através de blocos Position modify configurados para abrir apenas a partir de posição zerada, de modo que um sinal atrasado abre uma posição e impulsos repetidos na mesma direção não conseguem piramidar.
- Um terceiro bloco Position modify fecha o que estiver aberto, acionado por três fontes: o sinal atrasado de qualquer um dos lados e o sinalizador de fora da sessão; o painel de gráfico desenha os candles, a média, as duas bordas do corredor, as ordens e as execuções.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento cruza a borda superior do corredor de baixo para cima, o que arma o atraso de compra. Um número configurado de candles finalizados depois, o atraso libera o seu impulso; se o relógio estiver dentro da sessão nesse momento, o bloco Position modify de compra compra o volume da ordem a mercado. Como o bloco abre apenas a partir de posição zerada, a compra é ignorada quando já existe uma posição aberta — nesse caso o mesmo impulso já foi para o bloco de fechamento.
- **Entrada vendida**: O fechamento cai através da borda inferior do corredor, o que arma o atraso de venda. Um número configurado de candles finalizados depois, o impulso chega e, se o relógio estiver dentro da sessão, o bloco Position modify de venda vende o volume da ordem a mercado, novamente apenas a partir de posição zerada.
- **Saída**: Duas coisas encerram uma operação. Um sinal atrasado de qualquer um dos lados é ligado tanto ao bloco de fechamento quanto ao seu próprio bloco de entrada, de modo que uma reversão retida contra uma posição aberta a zera a mercado; a entrada seguinte espera o próximo sinal, porque os blocos de abertura só funcionam a partir de posição zerada. A outra é o relógio: assim que o sinalizador de horário de funcionamento fica falso, o bloco NOT dispara a cada candle e o bloco de fechamento zera a posição e a mantém zerada até a sessão abrir novamente. Com a posição zerada, o bloco de fechamento não faz nada. Aqui não há bloco de stop-loss nem de take-profit.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:30:00 | Time frame da única série de candles sobre a qual todo o diagrama funciona. |
| Average Length | 20 | Comprimento da média móvel que desenha o centro do corredor. |
| Corridor Sensitivity | 0.004 | Metade da largura do corredor como fração da média — o padrão é quatro décimos de por cento de cada lado. Aumente-o para reversões mais raras e mais amplas; reduza-o e as bordas passam a ser cruzadas com muito mais frequência. |
| Long Signal Delay | 2 | Candles finalizados contados entre a reversão de alta e o impulso que pode comprar. Um significa o candle seguinte; valores maiores seguram o sinal por mais tempo. |
| Short Signal Delay | 2 | Candles finalizados contados entre a reversão de baixa e o impulso que pode vender. |
| Session Start | 08:00:00 | Início da sessão de trabalho. Impulsos liberados antes dele são descartados e o diagrama permanece zerado. |
| Session End | 20:00:00 | Fim da sessão de trabalho. A partir desse momento, o sinalizador de fora da sessão zera qualquer posição aberta a cada candle até a sessão abrir novamente. |
| Order Volume | 1 | Tamanho da ordem, em lotes, enviado na entrada; o bloco de fechamento sempre zera o que estiver aberto. |

## Detalhes do diagrama

- Um bloco de cruzamento é um evento, não um estado. Ele fala apenas no candle em que as duas séries trocam de lugar, de modo que cada atraso é armado uma vez por reversão, em vez de ser rearmado a cada candle que o preço passa fora do corredor.
- O bloco de cruzamento também informa a direção oposta como um valor falso, e tanto a entrada de armamento do atraso quanto o gatilho da ordem ignoram valores falsos, de modo que a metade de baixa de um bloco de cruzamento não pode armar nem disparar o ramo de alta.
- Enquanto um atraso está contando, um segundo armamento é ignorado. Uma rajada de reversões produz, portanto, um impulso, e não uma fila deles, e a contagem é uma pausa de verdade, e não um acumulador.
- O bloco de candles emite somente candles finalizados. Uma atualização de um candle em formação carrega o horário de abertura da barra, e uma ordem construída a partir desse valor fica datada atrás do relógio do emulador e é rejeitada.
- A sessão é lida a partir do horário do próprio candle, e não do relógio do sistema, de modo que uma reprodução se comporta exatamente como uma execução ao vivo e o mesmo diagrama pode ser testado no histórico sem alterar nada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
