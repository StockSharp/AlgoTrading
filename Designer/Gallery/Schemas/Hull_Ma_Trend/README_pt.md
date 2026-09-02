# Diagrama da estratégia de inclinação da Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Hull Moving Average acompanha o preço com muito pouco atraso, então a direção da própria inclinação já é um sinal de tendência. O diagrama mede o quanto a média se moveu desde o candle anterior, como fração do próprio valor, e vira a posição para esse lado assim que o movimento supera um limiar pequeno. O original conta 500 candles de um minuto; aqui o comprimento é de 100 candles de cinco minutos, o mesmo intervalo de tempo no histórico incluído.

![schema](schema.svg)

## Visão geral da estratégia

- Negocia-se apenas a inclinação da Hull Moving Average: o preço nunca é comparado com a média.
- A inclinação é relativa, expressa como fração do valor anterior, de modo que o mesmo limiar funciona em qualquer nível de preço.
- Acima de +0,02% o diagrama quer estar comprado, abaixo de -0,02% vendido; dentro dessa faixa nada acontece e a posição aberta é mantida.
- Depois do primeiro sinal a estratégia está sempre no mercado: não há stop, alvo nem estado zerado entre as operações, exatamente como no código original.

## Regras de entrada e saída

- **Entrada comprada**: A Hull Moving Average subiu mais do que o limiar de alta desde o candle anterior e a posição não está comprada. A ordem compra o volume compartilhado mais o tamanho da venda aberta, de modo que uma ordem inverte a posição.
- **Entrada vendida**: A Hull Moving Average caiu mais do que o limiar de baixa desde o candle anterior e a posição não está vendida. A ordem vende o volume compartilhado mais o tamanho da compra aberta.
- **Saída**: Não há bloco de saída: o sinal de inclinação contrário inverte a posição e, como o volume da ordem já contém o módulo da posição, uma única ordem a mercado fecha um lado e abre o outro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Hull MA Length | 100 | Comprimento da Hull Moving Average, convertido de 500 candles de um minuto para 100 de cinco minutos. |
| Rise Threshold | 0.0002 | Alta relativa da média em um candle que abre uma compra; 0,0002 equivale a 0,02%. |
| Fall Threshold | -0.0002 | Queda relativa da média em um candle que abre uma venda; o espelho do limiar de alta. |
| Volume | 1 | Volume da ordem, em lotes, antes de somar a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um bloco de valor anterior guarda a Hull do candle anterior e fica em silêncio no primeiro valor, o que reproduz a primeira barra ignorada pelo original.
- A fórmula da inclinação subtrai o valor anterior do atual e divide pelo anterior, transformando o movimento em uma fração.
- Duas comparações dividem essa fração em três estados com as constantes de limiar positiva e negativa.
- Cada E lógico une uma condição de inclinação a uma verificação de posição, e a fórmula de volume soma o módulo da posição ao volume compartilhado, o que transforma uma entrada em uma inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
