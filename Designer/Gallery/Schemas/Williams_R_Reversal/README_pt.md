# Diagrama da estratégia de cruzamento de níveis do Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Williams %R mostra onde está o último fechamento entre a máxima e a mínima da janela recente, numa escala que vai de -100 no fundo até 0 no topo. O diagrama não negocia o tempo passado numa zona extrema, e sim o instante em que o indicador sai dela: a volta acima de -80 compra e a volta abaixo de -20 vende.

![schema](schema.svg)

## Visão geral da estratégia

- O Williams %R é calculado sobre candles finalizados de um único instrumento e equivale integralmente à fórmula de máxima e mínima que a estratégia original calcula à mão.
- Dois níveis dividem a escala: abaixo de -80 o mercado é considerado sobrevendido e acima de -20, sobrecomprado.
- Um bloco de valor anterior guarda a leitura do candle precedente, então cada nível é testado duas vezes e apenas o candle do cruzamento gera sinal.
- A posição atual participa das duas decisões, de modo que nenhuma ordem aumenta uma posição já aberta.

## Regras de entrada e saída

- **Entrada comprada**: A leitura anterior do %R estava abaixo do nível inferior, a atual está nele ou acima, e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda leva a posição de volta a zero.
- **Entrada vendida**: A leitura anterior do %R estava acima do nível superior, a atual está nele ou abaixo, e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra leva a posição de volta a zero.
- **Saída**: Não há bloco de saída próprio: o cruzamento contrário envia uma ordem a mercado do mesmo volume e zera a posição exatamente como na estratégia original. Esta ainda fica de fora por cinquenta candles após cada operação; aqui não existe um bloco contador de barras, então o cruzamento de nível assume sozinho esse papel e o diagrama negocia um pouco mais do que o código de origem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Williams %R Length | 14 | Janela de máxima e mínima sobre a qual o Williams %R é medido. |
| Lower Level | -80 | Nível que o indicador precisa recuperar para cima para dar sinal de compra. |
| Upper Level | -20 | Nível que o indicador precisa perder para baixo para dar sinal de venda. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador Williams %R, cuja saída vai tanto aos blocos de comparação quanto ao bloco de valor anterior.
- Quatro blocos de comparação montam os dois cruzamentos: a leitura anterior contra um nível e a leitura atual contra o mesmo nível.
- O bloco de posição é comparado duas vezes com uma constante zero, dando a proteção «não comprado» para a compra e «não vendido» para a venda.
- Cada E lógico une as duas metades de um cruzamento à sua proteção de posição e aciona um bloco de modificação de posição; ambos tiram o volume de uma constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
