# Diagrama da estratégia de desvio do spread entre dois instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois instrumentos que normalmente se movem juntos às vezes deixam de fazê-lo, e a diferença entre eles tende a se fechar de novo. O diagrama divide um preço pelo outro, observa o quanto essa razão se afasta da sua própria média móvel e compra ou vende o primeiro instrumento no momento em que a diferença começa a se fechar, e não no momento em que ela se abre.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de índice constrói um único instrumento sintético a partir de dois instrumentos reais, dividindo o preço do primeiro pelo preço do segundo, e uma série de candles é assinada para esse instrumento sintético, assim o spread chega como candles prontos em vez de ser montado à mão.
- Uma média móvel percorre os candles do spread e um conversor toma o preço de fechamento deles, o que dá ao spread tanto um nível atual quanto uma linha de referência própria.
- Uma fórmula reduz o par a um único número: a distância entre o fechamento e a média, em porcentagem da média.
- A mesma leitura uma barra atrás é reconstruída a partir de um candle anterior do spread e de um valor anterior da média, e uma segunda fórmula transforma esses dois no desvio da barra anterior.
- Uma segunda série de candles roda sobre o instrumento negociado, e duas variáveis prendem os dois desvios a ela: cada uma guarda o último número que o spread produziu e o libera quando um candle do instrumento negociado se fecha, assim toda decisão é tomada no relógio do instrumento para o qual as ordens vão.
- As comparações fazem então quatro perguntas: onde estava o desvio anterior em relação ao limite, para que lado o desvio está se movendo agora, de qual lado da média ele ainda está e se a posição está zerada. Uma condição lógica reúne as quatro respostas em um único portão de entrada por direção.
- As entradas são ordens a mercado de tamanho fixo tomadas apenas a partir de posição zerada, e são enviadas para o instrumento que a própria estratégia tem configurado. O instrumento sintético é uma fonte de números e nunca carrega uma ordem.
- Mais duas comparações observam o desvio voltar à média e entregam esse momento a dois blocos de posição, um para cada lado, que fecham o que estiver aberto.

## Regras de entrada e saída

- **Entrada comprada**: O desvio da barra anterior estava abaixo do limite inferior, o desvio desta barra é maior que o anterior, o desvio ainda está abaixo da média e a posição está zerada. O bloco long compra o volume da ordem a mercado.
- **Entrada vendida**: O desvio da barra anterior estava acima do limite superior, o desvio desta barra é menor que o anterior, o desvio ainda está acima da média e a posição está zerada. O bloco short vende o volume da ordem a mercado.
- **Saída**: Uma posição é fechada quando o spread completa o percurso pelo qual foi comprada: uma posição comprada sai quando o desvio alcança a média ou a cruza para cima, e uma vendida quando ele alcança a média ou a cruza para baixo. Cada bloco de fechamento carrega um lado próprio, portanto aquele destinado às compras não consegue tocar em uma venda, e um bloco de fechamento acionado com a posição zerada simplesmente não faz nada. Aqui não há alvo de lucro, stop-loss nem limite de tempo; o retorno do spread é toda a saída.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | A expressão a partir da qual o instrumento sintético é construído: o preço do primeiro instrumento dividido pelo preço do segundo. Os dois instrumentos precisam existir nos dados conectados, e apenas o primeiro deles é negociado. |
| Spread Candles | 00:05:00 | Duração dos candles sobre os quais a série do spread é construída. |
| Traded Candles | 00:05:00 | Duração dos candles sobre os quais as ordens são colocadas. Mantenha igual à série do spread, caso contrário os números retidos serão mais antigos que a barra em que são lidos. |
| Average Length | 20 | Número de candles do spread na média móvel a partir da qual o desvio é medido. |
| Deviation Threshold, % | 0.3 | O quanto o spread precisa se afastar da sua média, em porcentagem, antes que um retorno em direção a ela valha a pena ser negociado. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |

## Detalhes do diagrama

- O desvio da barra anterior é reconstruído a partir de um candle anterior e de um valor anterior da média, em vez de guardar a porcentagem já pronta, assim as duas metades da razão são lidas uma barra atrás e a comparação entre o agora e o então não pode misturar dois momentos diferentes.
- As duas séries não se encerram no mesmo instante: um instrumento sintético é montado a partir de dois fluxos de dados e o seu candle fecha um pouco depois do candle simples. Prender os números ao candle do instrumento negociado é o que mantém a estratégia em um único relógio; liberar as duas séries juntas dataria cada ordem pela mais antiga das duas, e uma ordem datada antes do horário atual é recusada.
- As constantes são disparadas pelo candle do instrumento negociado. Uma comparação precisa que os seus dois valores cheguem de novo a cada avaliação, portanto uma constante que não é reenviada trava a condição que ela alimenta sem dar nenhum sinal disso.
- O limite inferior não é uma segunda constante, e sim uma fórmula sobre o mesmo valor, para que a banda permaneça simétrica seja qual for o limite configurado.
- Os blocos de ordem estão instruídos a não esperar que a estratégia entre em operação, e as entradas estão configuradas para abrir apenas a partir de posição zerada, assim um sinal que se repete enquanto uma operação está em andamento não acrescenta nada a ela.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
