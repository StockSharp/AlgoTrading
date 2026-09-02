# Diagrama da estratégia de reversão do RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama opera contra os extremos do RSI, mas apenas no instante em que o índice vira: compra quando o RSI volta a subir acima do nível de sobrevenda e vende quando cai de novo abaixo do nível de sobrecompra. Uma única ordem carrega o volume necessário para inverter a posição, de modo que a estratégia fica zerada ou somente em um lado.

![schema](schema.svg)

## Visão geral da estratégia

- O índice de força relativa é calculado sobre candles finalizados, e um bloco de valor anterior guarda a leitura do candle anterior, permitindo identificar exatamente o candle em que o índice retorna à faixa normal.
- A SimpleMovingAverage de 50 candles vem da estratégia original: ela não escolhe direção, apenas adia a negociação até estar formada.
- A posição atual participa das duas decisões, e o volume da ordem é o volume base somado à posição aberta, de forma que uma ordem a mercado encerra e inverte de uma vez.

## Regras de entrada e saída

- **Entrada comprada**: A leitura anterior do RSI está abaixo do nível de sobrevenda, a atual está nesse nível ou acima, a SMA 50 está formada e a posição não está comprada. A ordem compra o volume base mais o tamanho de uma venda aberta, invertendo uma venda em compra ou abrindo uma compra a partir do zero.
- **Entrada vendida**: A leitura anterior do RSI está acima do nível de sobrecompra, a atual está nesse nível ou abaixo, a SMA 50 está formada e a posição não está vendida. A ordem vende o volume base mais o tamanho de uma compra aberta, invertendo uma compra em venda ou abrindo uma venda a partir do zero.
- **Saída**: Não existe bloco de saída próprio: o sinal de reversão contrário encerra a posição e abre o outro lado com a mesma ordem. A estratégia original também não tem stop nem take, e sua pausa de dez candles após cada operação não foi transposta, pois os blocos do diagrama não mantêm nível entre candles.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| SMA Length | 50 | Período da média móvel simples que controla o aquecimento. |
| Oversold | 30 | Nível acima do qual o índice precisa retornar para comprar. |
| Overbought | 70 | Nível abaixo do qual o índice precisa retornar para vender. |
| Volume | 1 | Volume base da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores, e o bloco de valor anterior na saída do RSI fornece a leitura do candle anterior.
- Cada lado usa dois blocos de comparação que testam a leitura anterior e a atual contra a constante de nível, reproduzindo literalmente a condição do código-fonte.
- A comparação da SMA com zero equivale à proteção do código original; como o bloco de indicador só emite valores formados, a negociação começa após cinquenta candles.
- Um bloco de fórmula soma o módulo da posição à constante de volume, e ambos os blocos de modificação de posição enviam ordens a mercado com esse volume.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
