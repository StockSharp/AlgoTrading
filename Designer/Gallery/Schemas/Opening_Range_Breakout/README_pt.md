# Diagrama da estratégia Opening Range Breakout (rompimento das Bandas de Bollinger com filtro de EMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O exemplo mantém o nome da estratégia original, mas nele não existe nenhum intervalo de abertura de sessão: o que se negocia de fato é um rompimento das Bandas de Bollinger confirmado por uma EMA lenta. A saída do preço para fora da banda é o gatilho, a EMA decide se o rompimento vai a favor ou contra o mercado, e a banda central traz a operação de volta.

![schema](schema.svg)

## Visão geral da estratégia

- As Bandas de Bollinger e uma EMA de 50 períodos são calculadas sobre os mesmos candles de meia hora, e toda decisão usa o fechamento de um candle finalizado.
- O rompimento só vale no sentido da tendência: acima da banda superior o fechamento precisa estar também acima da EMA, e abaixo da banda inferior precisa estar também abaixo dela.
- A banda central é a saída dos dois lados, então a operação dura exatamente o tempo em que o preço se mantém afastado da própria média. Não há stop nem alvo de lucro.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha acima da banda superior de Bollinger, esse mesmo fechamento está acima da EMA e a posição está zerada. O bloco de modificação compra a mercado o volume compartilhado.
- **Entrada vendida**: O candle fecha abaixo da banda inferior de Bollinger, esse mesmo fechamento está abaixo da EMA e a posição está zerada. O bloco de modificação vende a mercado o volume compartilhado.
- **Saída**: O primeiro fechamento abaixo da banda central encerra a compra e o primeiro acima encerra a venda; os dois blocos trabalham em modo de fechamento e só agem quando há algo a encerrar.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Length | 20 | Período de suavização das Bandas de Bollinger, que também é o da banda central. |
| Bollinger Width | 2 | Largura das bandas em desvios padrão; o código original fixa em dois. |
| EMA Length | 50 | Período da EMA que define em qual direção o rompimento pode ser negociado. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:30:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta as Bandas de Bollinger, a EMA e um conversor do preço de fechamento; outros três conversores separam a banda superior, a inferior e a central.
- Seis comparações cobrem toda a lógica: duas para as bandas, duas para o filtro da EMA e duas para o retorno à banda central.
- Os dois E lógicos de entrada exigem posição zerada, de modo que uma entrada nunca aumenta uma operação aberta; os blocos de encerramento ligam-se diretamente às comparações com a banda central.
- Duas coisas do original em C# não estão aqui: a pausa de 10 candles entre as ações, que não tem bloco no Designer, e a inversão imediata — este diagrama primeiro encerra na banda central e abre o lado oposto em um candle posterior.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
