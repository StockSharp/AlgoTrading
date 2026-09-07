# Diagrama da estratégia estocástica em três períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama combina o momentum formado do Stochastic(5,3) em velas de 60, 15 e 5 minutos. Uma vez por hora, sincroniza as três diferenças %K-%D e negocia uma virada do momentum de cinco minutos alinhada aos dois períodos superiores.

![schema](schema.svg)

## Visão geral da estratégia

- Três fluxos de velas concluídas fornecem dados de 60, 15 e 5 minutos e podem ser construídos a partir de períodos menores.
- Cada fluxo calcula Stochastic %K(5), suaviza-o com SMA(3) para obter %D e subtrai %D de %K.
- A diferença horária captura os últimos valores dos três fluxos; Sync libera um grupo completo de três valores em cada fechamento horário.
- A diferença anterior sincronizada de cinco minutos detecta uma virada na linha zero, enquanto a posição atual impede aumentar a exposição além de uma unidade.
- As duas ações são operações a mercado de uma unidade, e Strategy trades envia cada execução ao gráfico.

## Regras de entrada e saída

- **Entrada comprada**: Quando a diferença de entrada anterior está acima de zero, a atual está em zero ou abaixo, as duas diferenças superiores são positivas e a posição não é comprada, comprar uma unidade a mercado.
- **Entrada vendida**: Quando a diferença de entrada anterior está abaixo de zero, a atual está em zero ou acima, as duas diferenças superiores são negativas e a posição não é vendida, vender uma unidade a mercado.
- **Saída**: Não há um ramo separado de saída. Uma ação contrária válida de uma unidade zera uma posição oposta de uma unidade; um sinal válido posterior pode abrir a outra direção. O diagrama não tem stop-loss, take-profit nem intervalo de espera.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Higher Candles Series | 01:00:00 | Velas concluídas de 60 minutos para o cálculo superior e o pulso de decisão horário. |
| Higher Stochastic %K Length | 5 | Período do Stochastic %K do período superior. |
| Higher Stochastic %K Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; as velas superiores estão ligadas diretamente. |
| Higher Stochastic %D SMA Length | 3 | Período de suavização aplicado ao %K superior para obter %D. |
| Higher Stochastic %D SMA Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; o %K superior está ligado diretamente. |
| Middle Candles Series | 00:15:00 | Velas concluídas de 15 minutos para o cálculo do período intermediário. |
| Middle Stochastic %K Length | 5 | Período do Stochastic %K do período intermediário. |
| Middle Stochastic %K Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; as velas intermediárias estão ligadas diretamente. |
| Middle Stochastic %D SMA Length | 3 | Período de suavização aplicado ao %K intermediário para obter %D. |
| Middle Stochastic %D SMA Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; o %K intermediário está ligado diretamente. |
| Entry Candles Series | 00:05:00 | Velas concluídas de 5 minutos para o cálculo de entrada e o gráfico. |
| Entry Stochastic %K Length | 5 | Período do Stochastic %K do período de entrada. |
| Entry Stochastic %K Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; as velas de entrada estão ligadas diretamente. |
| Entry Stochastic %D SMA Length | 3 | Período de suavização aplicado ao %K de entrada para obter %D. |
| Entry Stochastic %D SMA Source | Not set | Nenhum campo alternativo de entrada do indicador está selecionado; o %K de entrada está ligado diretamente. |
| Order Volume | 1 | Volume de mercado fixo usado pelas duas ações. |

## Detalhes do diagrama

- Todos os indicadores emitem apenas valores formados. SMA(3) recebe o %K correspondente, portanto cada diferença é exatamente %K menos sua média de três valores.
- A diferença horária aciona três armazenadores numéricos antes de seus valores entrarem em Sync; assim, os armazenadores intermediário e de entrada fornecem as últimas leituras disponíveis.
- Sync limpa cada grupo completo e emite três valores alinhados. O bloco Previous value guarda uma diferença de entrada sincronizada para a próxima comparação horária.
- A posição é capturada junto com a decisão sincronizada. Um valor até zero permite comprar e um valor a partir de zero permite vender, impedindo o aumento na mesma direção.
- O gráfico recebe cinco fluxos: velas de cinco minutos, as três diferenças %K-%D contínuas e todas as execuções da estratégia.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
