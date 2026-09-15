# Diagrama da estratégia de painel de votação com dois instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um painel de votação sobre dois instrumentos. A cada hora encerrada, cada instrumento é pontuado contra a sua própria hora anterior em sete leituras de preço — abertura, máxima, mínima, fechamento, ponto médio, preço típico e fechamento ponderado — e cada leitura que subiu lança um voto de alta, cada uma que caiu, um voto de baixa. As duas pontuações são somadas em um único veredicto, e o instrumento negociado é comprado ou vendido apenas com base nesse veredicto. Não há nenhum indicador em todo o diagrama: a decisão inteira é construída a partir de preços de candles, aritmética e comparações.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de candles alimentam o diagrama. Um roda no instrumento da própria estratégia e é o que é negociado; o outro é apontado para um instrumento nomeado por uma variável Security, de modo que o segundo símbolo é uma configuração, e não uma decisão de ligação.
- Ambas as séries aceitam apenas candles encerrados. Um voto contado a partir de uma barra ainda em formação mudaria várias vezes dentro da hora e dataria a ordem que dele decorre com a abertura dessa hora.
- Em cada série, o Previous value guarda o candle inteiro um passo atrás, e conversores leem abertura, máxima, mínima e fechamento do candle atual e do candle guardado. O candle é guardado primeiro e os seus campos são lidos depois — essa é a ordem que devolve um valor em toda barra.
- Esses oito números entram em uma Formula por instrumento. Sete termos sign(), um por leitura, cada um devolvendo +1, 0 ou -1, são somados: o resultado são os votos de alta menos os votos de baixa e fica entre -7 e +7. sign é o que torna possível uma comparação dentro de uma fórmula, que não tem como devolver um verdadeiro ou falso próprio.
- A pontuação de referência é amostrada na barra negociada por um Variable com input-as-trigger desligado: a pontuação chega na entrada e espera ali, o candle negociado chega no gatilho e a libera. Dois fluxos nunca pulsam no mesmo instante, e é isso que coloca o segundo instrumento no relógio do instrumento que está sendo negociado.
- Uma segunda Formula soma as duas pontuações no veredicto do painel, entre -14 e +14, e o veredicto é desenhado no gráfico sob os candles junto com as duas pontuações que o compõem.
- Um único limiar governa os dois lados. Uma comparação testa o veredicto contra ele, uma fórmula de uma linha inverte o seu sinal, e uma segunda comparação testa o veredicto contra esse valor — assim, aumentar a configuração aperta na mesma medida o caso de compra e o de venda.
- A posição é fixada na barra por uma variável e comparada com zero de três maneiras: zerada admite uma entrada, comprada ou vendida admite uma saída. Quatro portas AND acionam quatro blocos Position modify — dois abrem, dois fecham — e um segundo painel do gráfico desenha o instrumento de referência ao lado do negociado.

## Regras de entrada e saída

- **Entrada comprada**: Em uma barra encerrada, o veredicto do painel está acima do limiar — os dois instrumentos juntos lançam uma maioria clara dos seus catorze votos para cima — e a posição está zerada. A porta de compra dispara e o Position modify compra o volume da ordem a mercado sob a condição Open position, de modo que uma barra seguinte que repita o veredicto não acrescenta nada à posição.
- **Entrada vendida**: O caso espelhado: o veredicto está abaixo do limiar negado, a maioria dos catorze votos aponta para baixo e a posição está zerada. A porta de venda dispara e o segundo Position modify vende o volume da ordem a mercado, novamente sob a condição Open position.
- **Saída**: Não há stop-loss nem take-profit. O mesmo painel que abriu a posição é o que a devolve: um veredicto abaixo do limiar negado enquanto a posição está comprada dispara a porta de saída, e o Close position devolve a posição inteira a mercado; um veredicto acima do limiar enquanto a posição está vendida faz o mesmo do outro lado. Como os blocos de fechamento fecham o que está aberto em vez de vender um tamanho fixo, uma saída nunca pode inverter a posição — uma virada do painel deixa primeiro o diagrama zerado, e a barra seguinte que ainda leia da mesma forma abre o novo lado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | O segundo instrumento cuja barra lança a outra metade dos votos; ele precisa estar disponível na conexão junto com o negociado. |
| Traded Candles | 01:00:00 | Time frame dos candles negociados: o compasso em que o veredicto é lido e as ordens são enviadas. |
| Reference Candles | 01:00:00 | Time frame dos candles de referência. Mantenha-o igual ao dos negociados, caso contrário as duas pontuações são contadas sobre intervalos de comprimentos diferentes e a soma deixa de significar o que diz. |
| Vote Threshold | 2 | Que maioria os dois instrumentos precisam formar antes de uma posição ser aberta, e o quanto o veredicto precisa oscilar no sentido contrário antes de ela ser devolvida. É contada em votos, dos catorze que o painel lança. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |

## Detalhes do diagrama

- Todo bloco de ordem está configurado para não exigir conexão online, de modo que o diagrama se comporta da mesma forma no histórico e em um fluxo ao vivo; deixado no seu padrão, o bloco reteria todas as transações em dados reproduzidos.
- As entradas levam a condição Open position e as saídas, a condição Close position com o lado oposto. Sem uma condição, um bloco agiria a cada mudança de posição pela qual fosse acionado, e um único veredicto viraria uma enxurrada de ordens.
- Toda constante — o zero, o limiar e o volume da ordem — é acionada pelo candle negociado. Uma variável emite no seu gatilho e não quando o seu valor é definido, então uma constante sem gatilho deixaria a comparação ao seu lado silenciosa durante toda a execução.
- Uma leitura que se repete exatamente não lança voto algum: sign devolve zero em um valor inalterado, então só o movimento real é contado, e uma hora parada em um instrumento simplesmente deixa o outro decidir.
- O limiar é o que transforma o painel de um gatilho sensível demais em um filtro. Em zero, o diagrama age na menor das maiorias e se inverte em quase toda barra; elevado, ele espera até que os dois instrumentos concordem com força suficiente, e a posição é mantida através das discordâncias no meio do caminho.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
