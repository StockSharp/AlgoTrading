# Diagrama da estratégia de cruzamento %K/%D do Stochastic em zonas extremas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O cruzamento das duas linhas do Stochastic é um sinal comum e ruidoso, por isso este diagrama só o aceita onde ele significa algo: o cruzamento de alta precisa ocorrer enquanto %K ainda está sobrevendido e o de baixa enquanto %K ainda está sobrecomprado. Cada sinal aceito inverte a posição, de modo que o diagrama está sempre comprado ou vendido e nunca apenas esperando.

![schema](schema.svg)

## Visão geral da estratégia

- Um único bloco do Stochastic Oscillator fornece as duas linhas; blocos conversores separam o seu valor em %K e %D.
- Um bloco de cruzamento compara as linhas: o seu sinal marca o cruzamento de alta e o mesmo sinal invertido por um bloco NÃO marca o de baixa.
- O filtro de zona é uma simples comparação de %K com as constantes de sobrevenda e sobrecompra, portanto um cruzamento no meio da faixa é ignorado.
- O volume da ordem é o volume base mais o valor absoluto da posição, o que encerra o lado contrário e abre o novo com uma única ordem a mercado.
- Apesar do nome da pasta da estratégia original, não há RSI nela nem stop loss; a pausa de cinco candles mantida após cada operação não tem equivalente em blocos e foi omitida.
- O original trabalha em candles de quinze minutos; o diagrama foi reduzido para candles de cinco minutos, de acordo com o histórico de amostra incluído.

## Regras de entrada e saída

- **Entrada comprada**: %K cruza acima de %D enquanto %K está abaixo do nível de sobrevenda e a posição ainda não está comprada. A ordem compra o volume base mais a venda em aberto, invertendo a posição para comprada.
- **Entrada vendida**: %K cruza abaixo de %D enquanto %K está acima do nível de sobrecompra e a posição ainda não está vendida. A ordem vende o volume base mais a compra em aberto, invertendo a posição para vendida.
- **Saída**: Não há bloco de saída próprio: a posição é mantida até surgir o cruzamento contrário na zona oposta, e essa ordem encerra a operação antiga e abre a nova.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| %K Length | 14 | Período de cálculo da linha %K do Stochastic. |
| %D Length | 3 | Período de suavização da linha %D, a média móvel de %K. |
| Oversold | 20 | Nível abaixo do qual um cruzamento de alta é aceito como compra. |
| Overbought | 80 | Nível acima do qual um cruzamento de baixa é aceito como venda. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta um único Stochastic Oscillator, e dois conversores extraem do seu valor as linhas %K e %D.
- O bloco de cruzamento dispara apenas no candle em que as linhas trocam de lugar, o que impede negociar em toda barra em que elas estejam separadas.
- Cada E lógico une o cruzamento, a comparação de zona e uma checagem de posição antes de acionar um bloco de modificação de posição.
- Um bloco de fórmula soma o volume base ao valor absoluto da posição e alimenta os dois blocos de ordem, de modo que uma ordem a mercado executa toda a inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
