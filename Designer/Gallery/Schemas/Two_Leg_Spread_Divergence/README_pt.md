# Diagrama da estratégia de divergência de spread de duas pernas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois instrumentos que costumam se mover juntos às vezes se movem em magnitudes diferentes, e aquele que ficou para trás tende a recuperar o atraso. O diagrama mede quanto cada um dos dois percorreu ao longo do mesmo número de barras, subtrai um valor do outro e, quando a diferença se abre o suficiente, compra a perna que ficou para trás e vende a perna que se adiantou no mesmo momento. Ambas as pernas são devolvidas quando a diferença volta a se fechar.

![schema](schema.svg)

## Visão geral da estratégia

- Uma variável Security nomeia o segundo instrumento, de modo que o par é uma configuração e não uma decisão de ligação, e é essa mesma variável que aponta para ele a segunda série de candles e todos os blocos de ordens da segunda perna. A primeira perna é o instrumento no qual a própria estratégia está definida.
- Ambas as séries de candles estão configuradas apenas para candles finalizados, de modo que um candle não finalizado nunca pode mover uma decisão nem datar uma ordem.
- O Sync mantém uma linha por instrumento e libera os dois candles juntos no compasso de cinco minutos. Os dois feeds chegam de forma independente e só depois dessa retenção os dois candles pertencem à mesma barra — o que é a única coisa que torna significativa a comparação entre os dois instrumentos.
- Para cada candle liberado um conversor pega o fechamento, um bloco Previous value guarda o candle um número fixo de barras atrás e um segundo conversor lê o fechamento desse candle mais antigo. O bloco guarda o próprio candle e o campo é lido depois: é essa sequência que garante um valor em todas as barras.
- Duas fórmulas transformam cada par de preços em um único número: quanto aquela perna percorreu desde a barra mais antiga, em porcentagem do preço mais antigo.
- Duas variáveis prendem essas duas porcentagens ao candle negociado: cada uma guarda o último valor produzido pelo par e o libera quando uma barra do instrumento negociado é finalizada, de modo que toda comparação, condição e ordem a jusante carregam o relógio do instrumento para o qual as ordens vão.
- Uma fórmula subtrai o percurso da segunda perna do percurso da perna negociada, o que é a divergência, e outra toma o tamanho dela independentemente do sinal, para a saída. As comparações confrontam a divergência com uma banda simétrica, os dois valores das pernas com zero e a posição com zero; dois And e um Or transformam as duas leituras de sinal em um único flag de correlação.
- Três condições lógicas montam as decisões e seis blocos de posição as executam: dois abrem um par em cada direção, uma ordem por instrumento, e dois fecham ambas as pernas. Cada bloco de abertura está configurado para agir apenas a partir de uma posição zerada em seu próprio instrumento, de modo que um sinal que se repita enquanto um par está em curso não acrescenta nada a ele.

## Regras de entrada e saída

- **Entrada comprada**: A divergência fica abaixo da borda inferior da banda — a perna negociada ficou para trás da segunda —, ambas as pernas percorreram na mesma direção ao longo do deslocamento e a perna negociada está zerada. Com esse único sinal dois blocos disparam juntos: um compra o volume da perna negociada a mercado no instrumento da estratégia, o outro vende o volume da segunda perna a mercado no instrumento indicado.
- **Entrada vendida**: A divergência fica acima da borda superior da banda — a perna negociada se adiantou em relação à segunda —, ambas as pernas percorreram na mesma direção ao longo do deslocamento e a perna negociada está zerada. O par espelhado de blocos vende o volume da perna negociada e compra o volume da segunda perna, ambos a mercado.
- **Saída**: Ambas as pernas são devolvidas juntas quando a diferença sobre a qual foram abertas se fechou: o tamanho da divergência, tomado sem o sinal, cai abaixo do limiar de saída enquanto um par está aberto. Dois blocos de fechamento disparam com esse único sinal, um por instrumento, e cada um calcula o próprio tamanho a partir do que encontra aberto, de modo que nenhum deles precisa de um volume próprio e um bloco de fechamento disparado sobre um instrumento zerado simplesmente não faz nada. Aqui não há meta financeira, nem stop-loss, nem limite de tempo; as duas pernas voltando a se juntar são toda a saída.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Second Instrument | TONUSDT@BNBFT | O instrumento no qual a segunda perna é negociada. Ele precisa existir nos dados conectados e ser um instrumento realmente negociável: ordens são enviadas para ele, não apenas lidas dele. |
| Traded Candles | 00:05:00 | Duração dos candles no instrumento da própria estratégia. As decisões e as ordens correm nesse compasso. |
| Second Leg Candles | 00:05:00 | Duração dos candles no segundo instrumento. Mantenha-a igual à da série negociada, ou as duas pernas serão medidas em intervalos de tempo diferentes e a diferença entre elas não significará nada. |
| Sync Interval | 00:05:00 | O compasso no qual a retenção libera os dois instrumentos. Ajuste-o à duração dos candles. |
| Traded Leg Shift | 12 | Quantas barras atrás é medido o percurso da perna negociada. Doze barras de cinco minutos equivalem a uma hora. |
| Second Leg Shift | 12 | A mesma contagem para a segunda perna. Mantenha as duas iguais: dois intervalos diferentes tornam a subtração sem sentido. |
| Divergence Threshold, % | 0.3 | Quão distantes os dois percursos precisam estar, em porcentagem, antes que valha a pena abrir o par. A banda é simétrica, portanto esse único valor define as duas bordas. |
| Exit Threshold, % | 0.1 | Quão próximos os dois percursos precisam voltar a ficar, em porcentagem, antes que ambas as pernas sejam devolvidas. Mantenha-o abaixo do limiar de entrada, ou um par será fechado na mesma barra em que foi aberto. |
| Traded Leg Volume | 1 | Tamanho da ordem no instrumento da estratégia, em lotes. |
| Second Leg Volume | 10 | Tamanho da ordem no segundo instrumento, em lotes. Os dois tamanhos são definidos de forma independente, portanto a proporção entre as pernas é uma configuração e não algo calculado a partir dos preços — escolha-a de acordo com as escalas de preço dos dois instrumentos e confira-a com o passo de volume do segundo, ou a ordem será arredondada para baixo. |

## Detalhes do diagrama

- O Sync é o que emparelha as barras, mas nada é enviado a partir da liberação dele. Dois feeds não terminam no mesmo instante, e um conjunto liberado pela retenção carrega o mais antigo dos dois horários; uma ordem datada antes do horário atual é recusada. As duas variáveis que prendem os valores ao candle negociado são o que mantém todo o fluxo a jusante em um único relógio.
- Vale conhecer a consequência dessa retenção: os valores sobre os quais uma decisão é tomada são o último par completo, portanto o par abre na barra seguinte àquela que produziu a leitura, e não dentro dela.
- Toda constante — zero, os dois limiares e os dois volumes — é acionada pelo candle negociado. Uma comparação precisa que ambos os seus valores cheguem novamente a cada avaliação, e uma constante que nunca é reenviada interrompe silenciosamente a condição que alimenta, sem nenhum sinal disso.
- A borda inferior da banda é uma fórmula sobre o limiar, e não uma segunda constante, de modo que a banda permanece simétrica qualquer que seja o limiar definido, e há um único número a alterar em vez de dois que podem se desencontrar.
- O bloco de posição lê apenas o instrumento negociado, e o valor dele é preso à barra da mesma forma que os valores das pernas. Ambas as pernas são abertas e fechadas pelos mesmos sinais, portanto o estado da perna negociada representa o estado do par.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
