# Diagrama da estratégia de virada após uma sessão perdedora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A ideia é a virada depois de uma sessão ruim: uma sessão que termina abaixo de onde abriu costuma deixar um repique para a seguinte, então o diagrama espera o mercado se recuperar acima da sua média móvel e compra essa recuperação, fazendo o inverso depois de uma sessão que fechou em alta. Apesar do nome, a estratégia original não contém nenhum filtro por dia da semana, e este diagrama também não.

![schema](schema.svg)

## Visão geral da estratégia

- Duas séries de candles trabalham lado a lado: a série de sessão decide para que lado pender e a série de negociação, mais rápida, define o momento da entrada.
- O veredito da sessão é uma única comparação entre o fechamento do candle de sessão e a sua própria abertura, portanto nada precisa ser lembrado entre candles.
- A média móvel simples na série de negociação serve de confirmação: depois de uma sessão perdedora só se compra quando o preço já voltou acima da média.
- Como o veredito chega uma vez por candle de sessão, o E lógico só pode disparar uma vez por sessão, que é exatamente a regra de uma entrada por sessão do original.

## Regras de entrada e saída

- **Entrada comprada**: A última sessão fechou abaixo da sua abertura, o candle de negociação fecha acima da média móvel simples e a posição está zerada. A ordem compra o volume compartilhado a mercado.
- **Entrada vendida**: A última sessão fechou acima da sua abertura, o candle de negociação fecha abaixo da média móvel simples e a posição está zerada. A ordem vende o volume compartilhado a mercado.
- **Saída**: A saída é pelo lado da média, e não por um alvo: um fechamento de volta abaixo da média encerra uma compra e um fechamento de volta acima encerra uma venda. Não há stop loss nem take profit, exatamente como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MA Period | 20 | Período da média móvel simples que confirma a virada na série de negociação. |
| Volume | 1 | Volume da ordem, em lotes. |
| Trading candles | 00:05:00 | Tempo gráfico em que entradas e saídas são cronometradas. |

## Detalhes do diagrama

- O bloco de candles de sessão alimenta dois conversores, um para a abertura e outro para o fechamento, e as duas comparações entre eles dão os sinais de sessão em queda e em alta.
- O bloco de candles de negociação alimenta a média móvel e um conversor do preço de fechamento; duas comparações colocam esse fechamento de um lado ou de outro da média.
- Cada E lógico une um sinal de sessão, um sinal de lado da média e a checagem de posição zerada antes de acionar um bloco de entrada com condição de abrir posição.
- Os blocos de saída ficam ligados diretamente às duas comparações com a média e trazem a condição de fechar posição, de modo que cada um zera apenas o seu próprio lado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
