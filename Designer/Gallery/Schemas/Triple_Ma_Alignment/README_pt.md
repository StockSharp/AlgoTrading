# Diagrama da estratégia de alinhamento de três EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Três blocos ExponentialMovingAverage de comprimentos bem diferentes são calculados sobre os mesmos candles, e o diagrama lê a ordem entre eles como a tendência. Curta acima da média e média acima da longa significa alta; a ordem inversa significa baixa. A estratégia está sempre posicionada e inverte o lado com uma única ordem.

![schema](schema.svg)

## Visão geral da estratégia

- Só o preço é usado: sem oscilador e sem filtro de volatilidade, apenas a posição relativa de três médias exponenciais.
- O estado de alta é curta acima da média e média acima da longa; o de baixa é curta no máximo igual à média e média no máximo igual à longa. No meio, com as médias embaralhadas, nada acontece.
- A posição atual condiciona cada entrada, então um alinhamento que dura centenas de candles gera exatamente uma ordem.
- Não há saída própria: o tamanho da ordem é o volume mais o módulo da posição, de modo que uma ordem encerra o lado antigo e abre o novo.

## Regras de entrada e saída

- **Entrada comprada**: A ExponentialMovingAverage curta está acima da média, a média acima da longa e a posição ainda não está comprada. A ordem compra o volume mais o módulo da posição: abre uma compra a partir do zero ou vira uma venda em compra.
- **Entrada vendida**: A ExponentialMovingAverage curta está no máximo igual à média, a média no máximo igual à longa e a posição ainda não está vendida. A ordem vende o volume mais o módulo da posição: abre uma venda a partir do zero ou vira uma compra em venda.
- **Saída**: Não existe bloco de saída próprio. A posição só é abandonada quando surge o alinhamento contrário, e o tamanho de inversão faz com que o diagrama não fique fora do mercado por nenhum candle.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Short EMA period | 100 | Comprimento da ExponentialMovingAverage mais rápida. |
| Middle EMA period | 250 | Comprimento da ExponentialMovingAverage intermediária. |
| Long EMA period | 500 | Comprimento da ExponentialMovingAverage mais lenta. |
| Volume | 1 | Volume base da ordem, em lotes; ao inverter soma-se o módulo da posição. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um único bloco de candles alimenta os três blocos de indicador, então as médias são sempre calculadas sobre os mesmos candles finalizados.
- Quatro blocos de comparação montam os dois estados: dois «maior que» estritos para a pilha de alta e dois «menor ou igual» para a de baixa, que é exatamente a negação usada no código original.
- Cada E lógico une as duas comparações de médias à posição comparada com uma constante zero e aciona um bloco de modificação de posição.
- Um bloco de fórmula soma o módulo da posição à constante de volume e alimenta os dois blocos de ordem — é isso que transforma uma entrada em inversão.
- Simplificações deliberadas: o original usa candles de um minuto e este diagrama usa de cinco, então os mesmos comprimentos cobrem cinco vezes mais tempo. O original ainda guarda se o alinhamento já existia no candle anterior; essa marca foi removida, porque a verificação de posição bloqueia igualmente uma entrada repetida. O stop de 2% declarado nunca é aplicado no código, por isso nenhum bloco de proteção é desenhado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
