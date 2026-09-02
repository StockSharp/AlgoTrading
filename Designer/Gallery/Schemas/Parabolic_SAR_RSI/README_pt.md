# Diagrama da estratégia Parabolic SAR + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Parabolic SAR define de que lado do mercado ficar, e o índice de força relativa apenas pode vetar uma entrada feita contra um movimento já esgotado. A mesma linha do SAR que abre a operação também a encerra, de modo que a saída acompanha a tendência em vez de ficar presa a um preço fixo.

![schema](schema.svg)

## Visão geral da estratégia

- O Parabolic SAR é calculado sobre candles finalizados e comparado com o preço de fechamento de cada candle: fechamento acima da linha indica tendência de alta, abaixo indica tendência de baixa.
- O índice de força relativa funciona como filtro brando, exatamente como no código original: a compra exige RSI abaixo do nível de sobrecompra e a venda exige RSI acima do nível de sobrevenda, então só as entradas feitas direto no extremo são barradas.
- As posições são abertas apenas a partir do zero, e a única saída é a passagem do preço para o outro lado do SAR — o diagrama não tem stop fixo nem alvo de lucro.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha acima do Parabolic SAR, o RSI ainda está abaixo do nível de sobrecompra e a posição está zerada. O bloco de modificação compra a mercado o volume compartilhado.
- **Entrada vendida**: O candle fecha abaixo do Parabolic SAR, o RSI ainda está acima do nível de sobrevenda e a posição está zerada. O bloco de modificação vende a mercado o volume compartilhado.
- **Saída**: A compra é encerrada assim que um candle fecha abaixo da linha do SAR, e a venda assim que um candle fecha acima dela; os dois blocos de encerramento operam com o tamanho atual da posição.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| RSI Overbought | 70 | Nível abaixo do qual o índice deve estar para permitir uma entrada comprada. |
| RSI Oversold | 30 | Nível acima do qual o índice deve estar para permitir uma entrada vendida. |
| SAR Acceleration | 0.02 | Fator de aceleração inicial do Parabolic SAR. |
| SAR Max acceleration | 0.2 | Limite superior do fator de aceleração do SAR. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o Parabolic SAR, o índice de força relativa e um conversor que lê o preço de fechamento.
- Duas comparações posicionam o fechamento em relação à linha do SAR, outras duas testam o índice contra as constantes e três comparam a posição com zero.
- Cada E lógico reúne uma condição de preço, uma de filtro e uma de posição antes de acionar um bloco de modificação; os blocos de encerramento usam o modo de fechamento e dispensam volume.
- A pausa de 130 candles que a estratégia em C# mantém após cada operação não tem bloco equivalente no Designer, por isso este diagrama volta a entrar mais cedo e negocia com mais frequência.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
