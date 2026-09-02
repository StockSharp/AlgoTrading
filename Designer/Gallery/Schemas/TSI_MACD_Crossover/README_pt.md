# Diagrama da estratégia de cruzamento do TSI com sua linha de sinal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O True Strength Index é momento suavizado duas vezes, por isso vira tarde mas raramente mente. Lido contra a sua própria linha de sinal exponencial, comporta-se como um MACD lento: o cruzamento indica a direção e a distância entre as linhas diz o quanto a virada é convincente. Este diagrama aceita apenas cruzamentos em que essa distância já supera um mínimo, o que separa uma troca real de comando de duas linhas que apenas se tocam.

![schema](schema.svg)

## Visão geral da estratégia

- Um único bloco de True Strength Index carrega as duas linhas; dois conversores extraem do mesmo valor a linha do índice e a sua linha de sinal.
- Um bloco de cruzamento compara as duas linhas e informa a direção do cruzamento; um NÃO lógico transforma a mesma saída no cruzamento para baixo.
- Uma fórmula mede a distância absoluta entre as linhas e uma comparação exige que ela alcance ao menos a distância mínima antes de o cruzamento ser aceito.
- A verificação de posição decide se a entrada é permitida, e o volume da ordem é o volume compartilhado mais a posição absoluta, de modo que um sinal contrário inverte com uma única ordem.

## Regras de entrada e saída

- **Entrada comprada**: A linha do TSI cruza a linha de sinal para cima, a distância entre elas alcança ao menos o mínimo e a posição não está comprada. A ordem compra o volume compartilhado mais o tamanho de uma venda aberta, de modo que uma única ordem a mercado encerra a venda e abre a compra.
- **Entrada vendida**: A linha do TSI cruza a linha de sinal para baixo, a distância entre elas alcança ao menos o mínimo e a posição não está vendida. A ordem vende o volume compartilhado mais o tamanho de uma compra aberta.
- **Saída**: Não há regra de saída própria nem stop de proteção, exatamente como no original: a posição é mantida até que o cruzamento contrário a inverta. Duas coisas foram simplificadas. O original espera dez candles depois de cada entrada antes de olhar novamente para os sinais, e nenhum bloco guarda um contador de barras entre candles, portanto essa pausa foi removida; a verificação de posição continua impedindo uma segunda entrada no mesmo sentido. O original também dispara duas ordens a mercado ao inverter, o que dobra o tamanho por um instante; aqui a fórmula de volume faz o mesmo em uma única ordem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| TSI First Length | 25 | Primeiro período de suavização do True Strength Index. |
| TSI Second Length | 13 | Segundo período de suavização do True Strength Index. |
| TSI Signal Length | 7 | Período da linha de sinal exponencial traçada sobre o índice. |
| Min spread | 2 | Distância absoluta mínima entre o índice e sua linha de sinal para o cruzamento valer. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 01:00:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. O original roda em candles de quatro horas; em um mês de histórico sobram poucas barras finalizadas para um índice duplamente suavizado se formar e ainda operar, então o diagrama foi reduzido para candles de uma hora. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco do True Strength Index, cujo valor complexo é dividido por dois conversores no índice e na sua linha de sinal.
- O bloco de cruzamento recebe o índice na entrada superior e a linha de sinal na inferior, então a sua saída é verdadeira no cruzamento para cima e falsa no cruzamento para baixo.
- A fórmula da distância e a sua comparação são calculadas em cada candle, enquanto o bloco de cruzamento só se manifesta nos cruzamentos, de modo que cada E lógico dispara exatamente na barra em que ocorre um cruzamento filtrado.
- Ambos os blocos de modificação de posição obtêm o volume de uma única fórmula que soma a posição absoluta à constante de volume compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
