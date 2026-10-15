# Diagrama da Estratégia de Z-Score do Spread Delta-Neutro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois instrumentos que normalmente se movem juntos às vezes se afastam, e a distância entre eles tende a se fechar de novo. O diagrama divide um preço pelo outro para obter um instrumento sintético, mede o quanto essa razão se afastou da própria média em unidades da própria volatilidade e abre os dois instrumentos ao mesmo tempo em direções opostas: comprado no lado barato contra vendido no lado caro. Quando a razão volta ao ponto em que costuma ficar, as duas pernas são liberadas. Cada entrada também é anunciada como uma linha de texto que carrega a leitura que a provocou.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de índice constrói um instrumento sintético a partir dos dois instrumentos reais, dividindo o preço do primeiro pelo preço do segundo, e uma série de candles é assinada nesse instrumento sintético, de modo que o spread chega como candles prontos em vez de ser montado à mão a partir de dois fluxos de dados.
- Uma média móvel e um desvio-padrão são calculados sobre os candles do spread, e um conversor extrai o preço de fechamento deles. Esses três números são tudo o que a decisão precisa: onde o spread está, onde ele costuma ficar e qual é a amplitude habitual das suas oscilações.
- Uma fórmula os transforma em um z-score, a distância do fechamento até a média dividida pelo desvio, e uma fórmula espelhada produz o mesmo valor com o sinal invertido, de modo que um único limiar de entrada e um único limiar de saída atendem às duas direções sem um segundo par de constantes.
- Mais duas séries de candles rodam sobre os dois instrumentos efetivamente negociados, e duas variáveis prendem as leituras ao instrumento negociado: cada uma guarda o último número produzido pelo spread e o libera quando um candle do instrumento negociado se fecha, de modo que toda decisão segue o relógio do instrumento para o qual as ordens são enviadas.
- Dois blocos de posição, um por instrumento, informam o que está aberto. Seus valores são presos à mesma barra do instrumento negociado e comparados com zero, o que dá ao diagrama quatro respostas simples: cada perna está zerada ou aberta.
- Comparações com o limiar de entrada dizem se o spread está muito abaixo ou muito acima da sua média, e uma condição lógica combina isso com o fato de ambas as pernas estarem zeradas. Só então quatro blocos de posição disparam juntos, comprando um instrumento e vendendo o outro no mesmo volume.
- Duas comparações com o limiar de saída, unidas por uma condição lógica, indicam que a leitura é pequena em termos absolutos, ou seja, que o spread voltou para perto da sua média. Cada perna tem o seu próprio gatilho de fechamento, de modo que uma perna já zerada nunca recebe ordem de fechar e uma perna ainda aberta sempre recebe.
- No momento da entrada, uma variável captura o z-score que a provocou, um formatador de texto o insere em uma frase e um bloco de notificação registra essa frase no log da estratégia. O painel de gráfico desenha os dois instrumentos negociados, a série sintética do spread, a média, o desvio e todas as ordens e execuções.

## Regras de entrada e saída

- **Entrada comprada**: O z-score espelhado está acima do limiar de entrada, o que significa que o spread está abaixo da sua própria média por mais desvios-padrão do que esse valor, e ambas as pernas estão zeradas. O diagrama compra o instrumento negociado e vende o instrumento de hedge, ambos a mercado e ambos no volume da ordem.
- **Entrada vendida**: O z-score está acima do limiar de entrada, o que significa que o spread está acima da sua própria média por mais desvios-padrão do que esse valor, e ambas as pernas estão zeradas. O diagrama vende o instrumento negociado e compra o instrumento de hedge, ambos a mercado e ambos no volume da ordem.
- **Saída**: As duas pernas são liberadas quando o z-score absoluto cai abaixo do limiar de saída, ou seja, quando o spread voltou para dentro de uma faixa estreita em torno da sua média. Os dois blocos de fechamento não trazem lado próprio e estão configurados para fechar a posição: cada um determina sozinho em que direção negociar e em que volume, de modo que o mesmo par de blocos desfaz tanto um spread comprado quanto um vendido. Além disso, a perna negociada conta com um stop-loss percentual medido a partir do preço da sua execução. Esse stop cobre apenas uma perna: uma posição pareada não pode ser fechada por uma única ordem de proteção, portanto a perna de hedge mantém o seu próprio gatilho de fechamento e sai quando o spread retorna.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | A expressão a partir da qual o instrumento sintético é construído: o preço do instrumento negociado dividido pelo preço do instrumento de hedge. Os dois instrumentos precisam existir nos dados conectados, e os dois são negociados. |
| Spread Candles | 00:05:00 | Duração dos candles sobre os quais a série do spread é construída e, portanto, da barra em que a média e o desvio são medidos. |
| Traded Leg Candles | 00:05:00 | Duração dos candles em que a perna negociada é acompanhada. Esse é o relógio pelo qual todo o diagrama funciona, então mantenha-a igual à da série do spread; caso contrário, as leituras presas serão mais antigas do que a barra em que são lidas. |
| Hedge Leg Candles | 00:05:00 | Duração dos candles em que a perna de hedge é acompanhada. A série existe para que os preços do instrumento de hedge cheguem à execução e apareçam no gráfico; mantenha-a igual às outras duas. |
| Average Length | 20 | Número de candles do spread na média móvel a partir da qual o z-score é medido. |
| Deviation Length | 20 | Número de candles do spread no desvio-padrão pelo qual o z-score é dividido. Mantenha-o igual ao comprimento da média, a menos que você queira deliberadamente um nível rápido contra uma amplitude lenta. |
| Entry Z-Score | 1.5 | Quantos desvios-padrão o spread precisa estar afastado da sua média antes de o par ser aberto. Aumentar esse valor torna as entradas mais raras e maior o afastamento em que elas são feitas. |
| Exit Z-Score | 0.5 | Quão perto da sua média o spread precisa voltar antes de as duas pernas serem liberadas. É um valor absoluto e cobre os dois lados, então o mesmo número fecha um spread comprado e um vendido. |
| Order Volume | 1 | Volume de cada perna, em lotes. As duas pernas são enviadas com o mesmo volume. |
| Stop Loss, % | 1.5 | Stop-loss da perna negociada, em porcentagem do preço em que ela foi executada. Protege apenas uma perna; a perna de hedge não tem stop próprio. |

## Detalhes do diagrama

- O instrumento sintético é uma razão, e não uma diferença. Uma diferença entre dois instrumentos com níveis de preço muito distintos é dominada pelo maior deles, e o z-score passaria então a medir esse instrumento isolado em vez da relação entre os dois.
- As duas séries não terminam no mesmo instante: um instrumento sintético é montado a partir de dois fluxos de dados e o seu candle fecha um pouco depois do de um instrumento comum. Prender as leituras ao candle do instrumento negociado mantém todo o diagrama em um único relógio; liberar as duas séries juntas dataria cada ordem pelo mais antigo dos dois momentos, e uma ordem datada antes do horário atual é rejeitada.
- As constantes são disparadas pelo candle do instrumento negociado. Uma comparação precisa que os seus dois valores cheguem novamente a cada avaliação, portanto uma constante que nunca é reenviada interrompe silenciosamente a condição que ela alimenta.
- A divisão que produz o z-score tem um piso em um número positivo muito pequeno, de modo que um trecho de preços completamente parados não consegue dividir por um desvio igual a zero e interromper a execução.
- Cada perna é acompanhada pelo seu próprio bloco de posição e controlada separadamente, de modo que, se o stop retirar primeiro a perna negociada, a perna de hedge ainda é liberada pela sua própria condição, em vez de ficar aberta até a entrada seguinte.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
