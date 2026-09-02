# Diagrama da estratégia Keltner RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama de reversão à média construído em torno da linha central de um canal de Keltner. O preço esticado abaixo da EMA com RSI fraco é comprado; o preço esticado acima dela com RSI forte é vendido, e a operação é devolvida quando o preço cruza a média de volta com o RSI passando do ponto médio. A estratégia original calcula as bandas ATR do canal mas nunca as lê, por isso este diagrama as deixa de fora e mantém apenas o que realmente decide uma operação.

![schema](schema.svg)

## Visão geral da estratégia

- A ExponentialMovingAverage de 20 períodos é a linha central do canal de Keltner e a única referência de preço de todo o diagrama.
- O RSI de 14 candles dá a segunda opinião: leitura abaixo de 45 confirma a liquidação que é comprada e acima de 55 confirma o impulso que é vendido.
- As duas entradas exigem posição zerada e as duas saídas são blocos de encerramento, de modo que os quatro ramos nunca disputam a mesma posição.
- Duas simplificações em relação ao original: as bandas ATR não utilizadas são removidas e a pausa de 120 barras após cada execução não tem bloco contador, então este diagrama negocia com mais frequência.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está abaixo da EMA, o RSI abaixo do nível de entrada comprada e a posição está zerada. A ordem compra o volume compartilhado a mercado e abre a compra.
- **Entrada vendida**: O fechamento está acima da EMA, o RSI acima do nível de entrada vendida e a posição está zerada. A ordem vende o volume compartilhado a mercado e abre a venda.
- **Saída**: A compra é encerrada quando o fechamento volta acima da EMA e o RSI passa do ponto médio; a venda é encerrada quando o fechamento volta abaixo da EMA e o RSI fica sob o ponto médio. Não há stop nem alvo, como no código original, em que o percentual de stop declarado nunca é aplicado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| EMA Length | 20 | Período da ExponentialMovingAverage que serve de linha central do canal. |
| RSI Length | 14 | Período de suavização do RelativeStrengthIndex. |
| RSI Long Entry | 45 | O RSI precisa estar abaixo deste nível para a entrada comprada. |
| RSI Short Entry | 55 | O RSI precisa estar acima deste nível para a entrada vendida. |
| RSI Exit Level | 50 | Ponto médio que o RSI deve ultrapassar para encerrar a posição. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta a EMA, o RSI e um conversor que lê o preço de fechamento.
- Dois blocos de comparação confrontam o fechamento com a EMA e outros quatro testam o RSI contra seus três níveis; o bloco de posição é comparado a uma constante zero.
- Dois E lógicos montam as entradas a partir de uma condição de preço, uma de RSI e a checagem de posição zerada, e acionam blocos de modificação em modo de abertura.
- Outros dois E lógicos montam as saídas e acionam blocos de modificação em modo de encerramento, que dispensam volume e só atuam sobre o lado que conseguem fechar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
