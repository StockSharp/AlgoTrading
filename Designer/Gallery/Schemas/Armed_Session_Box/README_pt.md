# Diagrama da estratégia Armed Session Box
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma noite calma quase sempre termina em algum lugar. Este diagrama mede a faixa que o mercado manteve entre duas horas da madrugada, espera a abertura da sessão de negociação e se arma uma única vez — congelando o topo e o fundo dessa caixa como os dois preços que irá negociar. A partir daí, acompanha as melhores cotações do livro de ofertas e compra ou vende no instante em que um dos níveis congelados é atingido.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos são assinados com atualizações intermediárias, e um bloco Final value deixa passar apenas os candles fechados; toda decisão do diagrama é tomada sobre uma barra concluída.
- Um bloco Working time delimita a janela de medição, e uma Variable do tipo candle é acionada por esse sinal, de modo que apenas os candles que caem dentro da janela noturna chegam aos indicadores — a caixa é construída a partir dessa janela e de mais nada.
- Highest e Lowest sobre o fluxo filtrado são o topo e o fundo da caixa, e duas variáveis guardam as últimas leituras para que o restante do diagrama possa consultar a caixa em qualquer candle, horas depois de ela ter sido medida.
- O Market depth é lido em busca da melhor oferta de venda e da melhor oferta de compra, e outras duas variáveis retêm essas cotações no ritmo dos candles: o livro é atualizado centenas de vezes por barra e nunca se alinharia a uma condição construída sobre candles.
- Fórmulas convertem a caixa em sua largura como percentual do preço e em uma margem de borda medida a partir dessa largura, de modo que os mesmos dois parâmetros signifiquem a mesma coisa em um instrumento cotado em dezenas de milhares e em outro cotado em unidades.
- Um segundo bloco Working time abre a sessão de negociação, e uma condição lógica reúne quatro respostas: a caixa é estreita, a oferta de venda está afastada do topo, a oferta de compra está afastada do fundo e a posição está zerada.
- O Flag transforma essa condição em um único evento de armação por sessão e congela ambos os níveis; a caixa por trás deles pode ser redesenhada na noite seguinte, mas os níveis armados não se movem.
- As entradas são ordens a mercado a partir de posição zerada, quando uma cotação retida atinge seu nível congelado, e o Position protection conduz a operação a partir daí.

## Regras de entrada e saída

- **Entrada comprada**: Dentro da sessão, com a caixa armada e a posição zerada, a melhor oferta de venda retida no candle atinge o nível superior congelado. O Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: Dentro da mesma sessão e sob as mesmas condições de caixa armada e posição zerada, a melhor oferta de compra retida no candle cai até o nível inferior congelado. O Position modify vende o volume da ordem a mercado.
- **Saída**: Não há sinal de saída no diagrama. O Position protection assume a operação assim que ela é aberta e a encerra em um take-profit ou em um stop-loss de um por cento sobre o preço de entrada, lendo o preço atual do livro de ofertas e não de um candle. Uma entrada aceita também devolve o estado de armação a zero, de modo que cada sessão dá uma operação e a próxima armação aguarda o dia seguinte.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual todo o diagrama funciona. |
| Box From | 02:00:00 | Início da janela em que a caixa é medida. |
| Box Until | 07:59:59 | Fim da janela de medição; o último candle que abre antes dele ainda conta. |
| Box Top Length | 72 | Sobre quantos candles da janela de medição é tomado o topo da caixa. |
| Box Bottom Length | 72 | Sobre quantos candles da janela de medição é tomado o fundo da caixa. |
| Max Box Width, % | 3 | Caixa mais larga, em percentual do preço, que ainda é considerada calma o bastante para negociar. |
| Edge Margin, % | 20 | A que distância de uma borda o preço precisa estar no momento da armação, em percentual da altura da caixa. |
| Session From | 08:00:00 | Início da sessão em que a armação e as entradas são permitidas. |
| Session Until | 20:00:00 | Fim da sessão; depois dele o Flag é liberado e o estado de armação é zerado. |
| Order Volume | 0.01 | Tamanho da ordem enviado pelas duas entradas. |
| Take Profit, % | 1 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- A caixa é um Highest e um Lowest móveis, do comprimento configurado, sobre os candles que caíram dentro da janela de medição, e não uma faixa reconstruída do zero a cada noite. No início da janela, a cauda da noite anterior ainda está no buffer; ao final da janela, as leituras correspondem exatamente à noite recém-medida, que é justamente quando elas são utilizadas.
- O estado de armação é mantido como um número, e não como a saída do Flag. O Flag emite seu único valor verdadeiro e depois permanece em silêncio, então o que as condições de entrada conseguem ler em cada candle é uma variável escrita como um na armação, como zero no fechamento da sessão e como zero após uma entrada.
- Todo valor que entra em uma comparação ou em uma condição lógica é reemitido a cada candle fechado por meio de uma variável de retenção. Uma comparação dispara apenas quando ambos os seus lados chegaram desde o último disparo, de modo que um nível capturado uma vez por dia precisa ser repassado adiante barra a barra.
- A armação é verificada em cada candle da sessão, e não apenas no seu primeiro minuto: o primeiro momento em que o preço se acomoda confortavelmente dentro de uma caixa estreita é o momento em que os níveis são congelados. Por isso, a armação e a primeira entrada possível estão sempre separadas por ao menos um candle.
- As entradas são ordens a mercado no toque de um nível, e a saída de proteção é um percentual do preço de entrada em vez da borda oposta da caixa, de modo que os dois lados da operação são expressos nas mesmas unidades e sobrevivem a uma troca de instrumento.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
