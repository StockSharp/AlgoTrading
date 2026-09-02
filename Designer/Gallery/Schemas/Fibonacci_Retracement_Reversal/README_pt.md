# Diagrama da estratégia de reversão em retrações de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A amplitude dos últimos vinte candles é dividida pela proporção áurea e os dois níveis de retração resultantes servem como zonas de reversão. Um candle que fecha sobre o nível inferior com corpo de alta é comprado, um que fecha sobre o nível superior com corpo de baixa é vendido, e a SimpleMovingAverage decide quando a operação termina.

![schema](schema.svg)

## Visão geral da estratégia

- Highest e Lowest na mesma janela dão a máxima e a mínima do movimento; a diferença entre elas é a amplitude em que os níveis são medidos.
- O nível de compra fica 0.618 da amplitude abaixo da máxima e o de venda 0.618 acima da mínima; um candle está sobre um nível enquanto seu fechamento estiver a menos de dois por cento da amplitude dele.
- As duas distâncias são calculadas como fração da amplitude, então o diagrama funciona igual em qualquer ativo e qualquer escala de preços.
- As entradas ainda exigem um corpo de candle que confirme e posição zerada; todas as saídas ficam por conta da SimpleMovingAverage, porque a estratégia original não define stop nem alvo.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento caiu dentro da margem em torno do nível de retração inferior, o candle é de alta (fechamento acima da abertura) e a posição está zerada. O bloco compra um lote e abre uma compra.
- **Entrada vendida**: O fechamento caiu dentro da margem em torno do nível de retração superior, o candle é de baixa (fechamento abaixo da abertura) e a posição está zerada. O bloco vende um lote e abre uma venda.
- **Saída**: A compra é encerrada assim que um candle fecha abaixo da SimpleMovingAverage e a venda assim que um fecha acima; os dois blocos operam em modo de fechamento e só disparam quando há posição para encerrar.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Swing lookback | 20 | Número de candles sobre os quais a máxima e a mínima do movimento são tomadas. |
| MA period | 20 | Período da SimpleMovingAverage contra a qual as saídas são medidas. |
| Fibonacci ratio | 0.618 | Razão de retração que posiciona os dois níveis dentro da amplitude. |
| Level buffer | 0.02 | Meia largura da zona de entrada em torno de um nível, como fração da amplitude. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um bloco de candles alimenta Highest, Lowest e a SimpleMovingAverage, além de dois conversores que extraem o fechamento e a abertura do candle.
- Dois blocos de fórmula transformam os preços na distância do fechamento até cada nível dividida pela amplitude, de modo que uma única constante de margem serve aos dois lados.
- Cada entrada passa por um E lógico de três sinais: o nível, o corpo do candle e a posição comparada com uma constante zero.
- Os dois blocos de saída são acionados diretamente pelas comparações com a média móvel e ficam em modo de fechamento; os quatro blocos de ordem compartilham uma constante de volume.
- Simplificações deliberadas: o original trabalha em candles de um minuto e faz uma pausa de 500 barras após cada operação, o que nenhum bloco expressa; por isso o diagrama usa candles de cinco minutos e volta a operar assim que as condições retornam. As posições duram algumas barras em vez de dias; aumentar o período da média as alonga.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
