# Diagrama da estratégia de referência de cara ou coroa aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia deliberadamente sem um sinal de mercado. Sempre que chega um candle de quatro horas concluído enquanto a posição está zerada, um valor aleatório escolhe entre uma entrada comprada e uma vendida. A posição é mantida por mais dez candles concluídos, fechada a mercado, e o ciclo pode recomeçar no candle seguinte. É uma referência educacional para comparar sistemas baseados em regras, não uma estratégia de negociação ao vivo.

![schema](schema.svg)

## Visão geral da estratégia

- Um único fluxo de candles de quatro horas, limitado a candles concluídos, marca tanto as decisões aleatórias quanto o contador do período de manutenção.
- O bloco Random produz um valor entre zero e um. Um limiar de 0.5 divide o intervalo em duas direções mutuamente exclusivas.
- A posição atual é registrada quando cada candle concluído chega e comparada com zero. Ambos os blocos de entrada também usam a condição Open position, portanto uma operação só pode começar se o diagrama estava sem posição no início do candle.
- A operação de entrada ativa um bloco N values, que conta dez candles concluídos subsequentes antes de permitir o fechamento da posição.
- O diagrama não usa indicadores, stop loss nem take profit. Sua sequência aleatória não tem uma semente configurada dentro do diagrama e pode variar entre execuções.

## Regras de entrada e saída

- **Entrada comprada**: O valor aleatório está abaixo do limiar da moeda e a posição está zerada. O diagrama compra a mercado o volume configurado.
- **Entrada vendida**: O valor aleatório é igual ou superior ao limiar da moeda e a posição está zerada. O diagrama vende a mercado o volume configurado.
- **Saída**: Depois que uma entrada é executada, o diagrama conta dez candles de quatro horas concluídos subsequentes. Em seguida, o bloco N values aciona o fechamento a mercado de toda a posição. O candle de saída não abre outra operação; o próximo candle concluído é a primeira oportunidade de entrar novamente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Hold Bars | 10 | Número de candles concluídos contados depois da execução de uma entrada e antes do fechamento da posição; o valor deve ser maior que zero. |
| Volume | 1 | Volume das ordens de entrada e saída, em lotes. O mesmo volume configurado é usado para abrir e reduzir a posição. |
| Coin Threshold | 0.5 | Valores aleatórios abaixo deste nível selecionam uma entrada comprada; valores iguais ou superiores selecionam uma entrada vendida. |
| Candles | 04:00:00 | Time frame de quatro horas usado nas decisões de entrada e na contagem do período de manutenção; somente candles concluídos são processados. |

## Detalhes do diagrama

- A saída de [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) alimenta o bloco [Random](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/random.html), aciona o registro da posição, entra no socket Input do bloco [N values](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) e chega ao painel do gráfico.
- Uma [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) verifica se o valor aleatório é pelo menos igual ao limiar da moeda. Esse sinal seleciona a ramificação vendida, enquanto uma [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) no modo NOT produz a ramificação comprada.
- A saída do bloco [Position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) alimenta uma Variable acionada que registra a posição no início do candle. O valor registrado é comparado com uma constante zero compartilhada, e seu sinal de posição zerada se combina com o sinal de direção em cada bloco AND de entrada. Esse registro impede que o candle de saída abra uma nova posição imediatamente após a execução do fechamento.
- Ambos os blocos de entrada [Modify position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) usam ordens a mercado com a condição Open position e recebem seu volume de uma constante compartilhada.
- As saídas MyTrade dos blocos de entrada comprada e vendida alimentam o socket Trigger do bloco N values. Acionamentos posteriores são ignorados enquanto a contagem de dez candles está ativa.
- A saída de N values aciona dois blocos Modify position no modo Reduce only. O bloco de venda só pode reduzir uma posição comprada, e o bloco de compra só pode reduzir uma posição vendida; ambos recebem o volume compartilhado, portanto apenas a ramificação aplicável registra uma saída.
- O [painel do gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe o fluxo de candles e as operações produzidas pelos dois blocos de entrada e pelos dois blocos de saída.

## Uso

Importe o arquivo `.json` no Designer e execute-o no backtester com dados históricos; depois, compare seus resultados com diagramas baseados em regras no mesmo instrumento e período. Use este exemplo como referência educacional, não como um sistema de negociação ao vivo.
