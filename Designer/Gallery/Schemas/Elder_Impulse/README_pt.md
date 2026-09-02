# Diagrama da estratégia Elder Impulse System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Alexander Elder colore cada barra por duas coisas ao mesmo tempo: a inclinação de uma média móvel exponencial, que mostra a tendência, e a inclinação do histograma do MACD, que mostra a força por trás dela. Quando ambas apontam para cima a barra é verde e o diagrama compra; quando ambas apontam para baixo a barra é vermelha e ele vende. As ordens têm tamanho igual a Volume mais a posição aberta, de modo que cada sinal inverte o que estiver em carteira.

![schema](schema.svg)

## Visão geral da estratégia

- A EMA e as linhas do MACD são calculadas sobre candles finalizados de um único instrumento; o histograma é montado dentro do próprio diagrama como MACD menos Signal.
- Dois blocos de valor anterior guardam a EMA e o histograma do candle passado, permitindo comparar a leitura atual com eles e definir para onde cada um se inclina.
- A cor da barra é o par de inclinações: EMA subindo e histograma subindo dá verde; EMA caindo e histograma parado ou caindo dá vermelho; o resto é neutro e é ignorado.
- A estratégia original fica de fora por 65 barras depois de cada operação. Essa pausa é um contador e os blocos do Designer não guardam esse estado, então o diagrama a omite; a verificação da posição já impede repetir o mesmo lado.

## Regras de entrada e saída

- **Entrada comprada**: A EMA está acima do seu valor de um candle atrás, o histograma também está, e a posição ainda não está comprada. A ordem compra Volume mais o módulo da posição: abre uma compra a partir do zero ou inverte uma venda de uma só vez.
- **Entrada vendida**: A EMA está abaixo do seu valor de um candle atrás, o histograma está nesse valor ou abaixo, e a posição ainda não está vendida. A ordem vende Volume mais o módulo da posição, abrindo uma venda ou invertendo uma compra.
- **Saída**: Não existe saída própria: a cor contrária inverte a posição e, como o tamanho da ordem inclui a posição aberta, a inversão fecha a operação antiga e abre a nova ao mesmo tempo. A estratégia de origem também não tem stop nem alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| EMA Length | 13 | Período da média móvel exponencial cuja inclinação colore a barra. |
| MACD Fast Length | 12 | Média móvel rápida do MACD. |
| MACD Slow Length | 26 | Média móvel lenta do MACD. |
| MACD Signal Length | 9 | Período da linha de sinal; o histograma é o MACD menos essa linha. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta dois blocos de indicador, EMA e MACD com linha de sinal; dois conversores extraem os valores MACD e Signal e um bloco de fórmula os subtrai para formar o histograma.
- Dois blocos de valor anterior, um tipado como valor de indicador e outro como número, entregam as leituras do candle anterior a quatro blocos de comparação que resolvem as duas inclinações.
- Cada E lógico junta uma condição da EMA, uma do histograma e uma da posição, de modo que só se entra quando a ordem não aumenta o lado já mantido.
- Um bloco de fórmula soma o módulo da posição à constante de volume compartilhada e alimenta os dois blocos de modificação de posição — é isso que transforma cada sinal em uma inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
