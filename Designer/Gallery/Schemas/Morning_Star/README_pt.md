# Diagrama da estratégia de reversão Morning Star
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Morning Star é o fundo clássico de três candles: um candle de baixa largo, um candle pequeno e hesitante e um candle de alta largo que recupera mais da metade do primeiro. Sua imagem espelhada, o Evening Star, marca um topo. Este diagrama reconhece as duas figuras com blocos de padrões de candle, abre posição apenas quando está zerado e devolve a operação assim que o preço fecha do lado errado de uma média móvel simples.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de indicador de padrões de candle carregam expressões próprias de três candles: o primeiro candle tem corpo e aponta contra a entrada futura, o corpo do meio é menor que metade dele e o terceiro fecha além do ponto médio do primeiro.
- Uma média móvel simples do preço de fechamento é a única referência de saída; o diagrama não tem stop loss nem take profit, exatamente como a estratégia original.
- O bloco de posição é comparado com zero, de modo que o padrão só é executado a partir do zero e nunca aumenta uma operação aberta.
- A estratégia original ainda congela todos os sinais por várias centenas de barras após cada execução; aqui não existe bloco contador de barras, então essa pausa foi omitida e está registrada.

## Regras de entrada e saída

- **Entrada comprada**: O bloco Morning Star informa o padrão no candle recém-encerrado e a posição é zero. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O bloco Evening Star informa o padrão no candle recém-encerrado e a posição é zero. A ordem vende um lote e abre uma venda.
- **Saída**: Uma compra é encerrada por um bloco de modificação de posição em modo de fechamento assim que um candle fecha abaixo da média móvel; uma venda é encerrada do mesmo jeito quando um candle fecha acima dela. Não há stop de proteção, porque a estratégia de origem também não tem.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples que encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles de todo o diagrama; o original roda em candles de um minuto e aqui foi ajustado ao histórico de cinco minutos que acompanha a galeria. |

## Detalhes do diagrama

- O bloco de candles alimenta quatro ramos: os dois indicadores de padrão, a média móvel e um conversor que lê o preço de fechamento.
- Cada bloco de padrão guarda uma expressão de três condições, então a figura é reconhecida sem uma corrente de blocos de fórmula.
- Dois blocos de comparação colocam o fechamento de um lado ou de outro da média e acionam diretamente os dois blocos de fechamento.
- Cada E lógico une um padrão à verificação de posição e dispara uma entrada; as duas ordens de entrada tiram o volume de uma constante compartilhada, enquanto os blocos de fechamento o calculam pela posição aberta.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
