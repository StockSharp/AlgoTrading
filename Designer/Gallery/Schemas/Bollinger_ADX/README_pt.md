# Diagrama da estratégia de rompimento das Bandas de Bollinger com ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um rompimento só vale a pena quando o mercado realmente vai a algum lugar. Este diagrama espera um fechamento fora de uma banda de Bollinger — sinal de que o movimento é grande demais para a volatilidade recente — e pergunta ao ADX se há uma tendência por trás. Se ambos concordarem, a posição é aberta no sentido do rompimento e abandonada assim que o preço volta à banda do meio.

![schema](schema.svg)

## Visão geral da estratégia

- As Bandas de Bollinger são calculadas sobre candles finalizados de um único instrumento: a superior e a inferior marcam os níveis de rompimento e a do meio, que é a média móvel do mesmo período, marca a saída.
- O ADX mede a força da tendência sem dizer nada sobre a direção, por isso é usado apenas como filtro: abaixo do limiar todo rompimento é ignorado.
- A posição atual participa das duas entradas, e os dois blocos de encerramento estão no modo de fechar em vez de abrir, de modo que cada um só age no seu lado.
- A estratégia de origem se trava por cem barras depois de qualquer operação, inclusive as saídas. Esse contador não tem equivalente entre os blocos, então o diagrama o omite: a saída na banda do meio passa a funcionar sempre, o que é mais sensato de todo modo.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está acima da banda superior, o ADX está acima do seu limiar e a posição está zerada. Compra-se um lote a mercado.
- **Entrada vendida**: O fechamento está abaixo da banda inferior, o ADX está acima do seu limiar e a posição está zerada. Vende-se um lote a mercado.
- **Saída**: Uma compra é encerrada no primeiro fechamento abaixo da banda do meio e uma venda no primeiro acima dela. Não há stop nem alvo, exatamente como na estratégia de origem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Length | 20 | Período de suavização das Bandas de Bollinger e da sua linha central. |
| Bollinger Width | 2.0 | Multiplicador do desvio padrão que define a largura das bandas. |
| ADX Length | 14 | Período do Índice Direcional Médio (ADX). |
| ADX Threshold | 25 | Nível acima do qual o ADX é considerado forte o bastante para negociar o rompimento. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta dois blocos de indicador e um conversor do preço de fechamento; outros três conversores extraem a banda superior, a inferior e a do meio de um mesmo valor de Bollinger, e mais um extrai a linha do ADX.
- Cinco blocos de comparação fazem o trabalho: dois para o rompimento, dois para a volta à banda do meio e um para o filtro de tendência contra uma constante de limiar.
- Cada E lógico junta uma condição de rompimento, o filtro de tendência e a verificação da posição e então aciona um bloco de modificação no modo de abrir, que tira o volume da constante compartilhada.
- As duas comparações de saída acionam blocos de modificação no modo de fechar, que dispensam volume próprio porque o bloco encerra o que estiver aberto.
- O código original calcula a força da tendência à mão, como um DX sem suavização. O diagrama usa o ADX padrão, a versão suavizada por Wilder da mesma grandeza, de modo que os momentos de cruzar o limiar diferem um pouco.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
