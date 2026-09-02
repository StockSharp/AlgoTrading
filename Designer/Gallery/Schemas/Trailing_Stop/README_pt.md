# Diagrama da estratégia Trailing Stop (cruzamento de EMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama de tendência curto cujo interesse está na saída, e não na entrada. Duas médias móveis exponenciais escolhem o lado, mas a parte de sinal nunca fecha uma operação: os blocos de modificação de posição apenas abrem, e é um bloco de proteção que leva a operação até o take-profit ou o stop-loss. A chave de trailing desse bloco fica desligada, porque a estratégia original declara uma distância de trailing e nunca a utiliza.

![schema](schema.svg)

## Visão geral da estratégia

- Uma ExponentialMovingAverage rápida e outra lenta são calculadas sobre a mesma série de candles.
- A entrada só ocorre a partir de posição zerada, de modo que uma operação aberta nunca é invertida nem aumentada.
- Os dois blocos de entrada enviam suas próprias execuções ao bloco de proteção, que coloca take-profit e stop-loss como percentual do preço executado.
- Esse bloco de proteção é a única saída da operação; o diagrama não tem sinal de saída próprio.

## Regras de entrada e saída

- **Entrada comprada**: A EMA rápida cruza acima da lenta com a posição exatamente em zero. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: A EMA rápida cruza abaixo da lenta com a posição exatamente em zero. A ordem vende um lote e abre uma venda.
- **Saída**: O bloco de proteção fecha a posição no take-profit de 2% ou no stop-loss de 1% em relação ao preço de entrada. Até que um deles seja atingido, o cruzamento contrário é ignorado, pois a entrada exige posição zerada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA Length | 6 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 18 | Período da média móvel exponencial lenta. |
| Take Profit, % | 2 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em percentual do preço de entrada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois blocos de indicador e ainda fornece o preço acompanhado pelo bloco de proteção.
- O bloco de cruzamento emite verdadeiro quando a EMA rápida sobe acima da lenta e falso quando cai abaixo, então um NÃO lógico obtém o sinal de venda da mesma saída.
- Uma única comparação com a constante zero basta como verificação de posição, e os dois blocos de modificação ainda operam em modo somente-abertura.
- As execuções próprias dos dois blocos de entrada entram no bloco de proteção: é isso que transforma um preenchimento em um par de ordens de ganho e de perda.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
