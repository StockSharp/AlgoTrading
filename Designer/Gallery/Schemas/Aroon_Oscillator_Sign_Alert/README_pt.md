# Diagrama da estratégia de mudança de sinal do Aroon Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Aroon Oscillator pergunta o que é mais recente, a máxima mais alta ou a mínima mais baixa dos últimos candles, e responde com um número entre -100 e +100. Este diagrama não opera o extremo em si, mas o instante em que o mercado sai dele: uma leitura que volta acima do nível inferior compra, e uma que cai abaixo do nível superior vende. A estratégia original usa candles de quatro horas; o diagrama trabalha em cinco minutos para que o mês de histórico incluído tenha barras suficientes para operar.

![schema](schema.svg)

## Visão geral da estratégia

- O AroonOscillator é calculado sobre candles finalizados de um único instrumento e oscila entre -100 e +100.
- Um bloco de valor anterior guarda a leitura do candle passado, de modo que um cruzamento real do nível se distingue de uma barra que apenas permanece acima dele.
- Os dois lados são propositalmente assimétricos: compra-se quando um forte viés de baixa perde força e vende-se quando um forte viés de alta perde força.
- A posição atual participa das duas decisões, portanto nenhuma ordem aumenta uma posição já aberta.

## Regras de entrada e saída

- **Entrada comprada**: A leitura anterior do AroonOscillator estava no nível inferior ou abaixo, a atual está acima dele e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: A leitura anterior do AroonOscillator estava no nível superior ou acima, a atual está abaixo dele e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: Não há bloco de saída nem stop de proteção, como na estratégia original: o sinal contrário zera a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Aroon Length | 9 | Quantidade de candles que o Aroon Oscillator olha para trás. |
| Down Level | -50 | Nível inferior; cruzá-lo de baixo para cima é o sinal de compra. |
| Up Level | 50 | Nível superior; cruzá-lo de cima para baixo é o sinal de venda. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles de todo o diagrama; o original usava quatro horas. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador com o AroonOscillator, e o bloco de valor anterior toma a mesma saída um candle atrás.
- Quatro blocos de comparação montam os dois cruzamentos: o valor anterior contra um nível e o atual contra o mesmo nível.
- Outros dois blocos comparam a posição com uma constante zero, e cada E lógico reúne três condições em um sinal.
- Ambos os blocos de modificação de posição enviam ordens a mercado e obtêm o volume de uma única constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
