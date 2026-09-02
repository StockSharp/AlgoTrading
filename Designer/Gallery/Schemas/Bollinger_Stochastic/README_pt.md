# Diagrama da estratégia Bandas de Bollinger + Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma reversão à média que exige dois sinais independentes de movimento esgotado: o fechamento precisa alcançar uma banda de Bollinger e a linha %K do Stochastic precisa estar na zona extrema correspondente. A posição é devolvida assim que o preço cruza a banda central das mesmas bandas, de modo que a operação dura exatamente o tempo do desvio.

![schema](schema.svg)

## Visão geral da estratégia

- As Bandas de Bollinger fornecem três linhas a partir de um único bloco de indicador: banda superior, banda inferior e a média central que serve de nível de saída.
- Do Stochastic usa-se apenas a linha %K; a linha %D fica propositalmente desconectada, como na estratégia original.
- As entradas só ocorrem a partir de posição zerada, portanto o diagrama nunca aumenta uma operação já aberta.
- A estratégia original ainda espera um número fixo de candles entre operações; esse contador não tem equivalente em blocos e foi omitido, o que faz este diagrama operar com mais frequência que o código de origem.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está na banda inferior de Bollinger ou abaixo dela, %K está abaixo do nível de sobrevenda e a posição está zerada. A ordem compra um lote e abre uma posição comprada.
- **Entrada vendida**: O fechamento está na banda superior de Bollinger ou acima dela, %K está acima do nível de sobrecompra e a posição está zerada. A ordem vende um lote e abre uma posição vendida.
- **Saída**: A compra é encerrada quando o fechamento sobe acima da banda central e a venda quando cai abaixo dela. As duas saídas usam blocos de modificação de posição em modo de fechamento: calculam o volume pela posição aberta e ficam inertes quando não há o que fechar. Não há stops nem alvos, exatamente como no código original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Bollinger Length | 20 | Período de suavização das Bandas de Bollinger, que também define a linha central usada na saída. |
| Bollinger Width | 2 | Multiplicador do desvio padrão que determina a distância das bandas até a linha central. |
| %K Oversold | 20 | Nível abaixo do qual a linha %K confirma uma compra. |
| %K Overbought | 80 | Nível acima do qual a linha %K confirma uma venda. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um único bloco de candles alimenta as Bandas de Bollinger, o Stochastic e um conversor que extrai o preço de fechamento.
- Blocos conversores separam os indicadores em linhas individuais: banda superior, inferior, central e %K.
- Cada E lógico une uma condição de banda, uma condição do Stochastic e a checagem de posição zerada antes de acionar um bloco de modificação de posição em modo de abertura.
- Os dois blocos de saída são acionados diretamente pelas comparações com a banda central; o modo de fechamento do próprio bloco decide se a ordem é mesmo necessária.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
