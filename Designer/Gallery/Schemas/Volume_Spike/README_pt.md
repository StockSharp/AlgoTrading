# Diagrama da estratégia de pico de volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um candle que carrega muito mais volume do que o anterior geralmente significa que alguém acabou de operar em tamanho. Este diagrama espera esse salto, deixa uma média móvel simples dizer se a maioria está comprando ou vendendo e acompanha enquanto o volume continuar crescendo. No momento em que o volume cai abaixo do candle anterior, a operação termina.

![schema](schema.svg)

## Visão geral da estratégia

- O volume do candle é comparado com o volume do candle anterior, e não com uma média de vários candles, exatamente como faz o código original.
- A comparação está escrita como multiplicação e não como divisão, de modo que um candle sem volume algum não quebra o diagrama.
- Uma média móvel simples de vinte candles sobre o preço de fechamento escolhe o lado: acima dela o pico é comprado, abaixo dela é vendido.
- As entradas ocorrem apenas a partir da posição zerada, e a saída não precisa nem da média nem do pico, apenas de um volume que parou de crescer.

## Regras de entrada e saída

- **Entrada comprada**: O volume do candle é pelo menos o multiplicador vezes o volume do candle anterior, o candle fechou acima da média móvel e a posição está zerada. A ordem compra um lote a mercado.
- **Entrada vendida**: O volume do candle é pelo menos o multiplicador vezes o volume do candle anterior, o candle fechou abaixo da média móvel e a posição está zerada. A ordem vende um lote a mercado.
- **Saída**: Os dois lados saem no primeiro candle cujo volume é menor que o do candle anterior, por blocos de modificação de posição em modo de fechamento. A estratégia original não tem stop loss nem take profit, e este diagrama também não.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Spike Multiplier | 2 | Quantas vezes o volume do candle anterior o candle atual precisa carregar para o pico valer. |
| SMA Length | 20 | Período da média móvel simples que escolhe o lado da entrada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta um conversor de volume, um conversor do preço de fechamento e o bloco da média móvel; um bloco de valor anterior deslocado em um candle entrega o volume do candle passado.
- Uma fórmula multiplica esse volume anterior pela constante do multiplicador, e um bloco de comparação confronta o volume atual com o resultado.
- Cada E lógico une o pico, o lado escolhido pela média móvel e a checagem de posição zerada, e aciona um bloco de modificação de posição no modo somente abertura.
- A comparação de volume em queda vai direto para os dois blocos de fechamento, que estão em modo de fechamento e por isso nada fazem enquanto o diagrama está zerado. O original ainda faz uma pausa de quinhentos candles após cada operação e trabalha em candles de um minuto; não existe bloco contador para essa pausa e o histórico empacotado é mais grosso que um minuto, então o diagrama roda em candles de cinco minutos e negocia todo pico.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
