# Estratégia Genie Stoch RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera utilizando uma combinação do Índice de Força Relativa (RSI) e do Oscilador Estocástico.
Ela aguarda o mercado atingir zonas de sobrecompra ou sobrevenda e, em seguida, procura um cruzamento entre a linha
principal do Estocástico e sua linha de sinal para confirmar a reversão. Um trailing stop e um take profit fixo são
aplicados para gestão de risco.

## Lógica

1. Subscrever velas do período selecionado.
2. Calcular o RSI com um período configurável.
3. Calcular o Oscilador Estocástico com períodos %K, %D e de desaceleração configuráveis.
4. Para uma entrada comprada:
   - O RSI está abaixo do nível de sobrevenda.
   - %K está abaixo do nível de sobrevenda do Estocástico.
   - O %K anterior está abaixo do %D anterior e o %K atual cruza para cima o %D atual.
5. Para uma entrada vendida:
   - O RSI está acima do nível de sobrecompra.
   - %K está acima do nível de sobrecompra do Estocástico.
   - O %K anterior está acima do %D anterior e o %K atual cruza para baixo o %D atual.
6. O tamanho da posição é retirado da propriedade `Volume` da estratégia. Posições existentes são invertidas quando um
   sinal contrário aparece.
7. `StartProtection` habilita um trailing stop e take profit medidos em pontos de preço.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `RsiPeriod` | Comprimento do cálculo do RSI. |
| `KPeriod` | Período %K do Estocástico. |
| `DPeriod` | Período %D do Estocástico. |
| `Slowing` | Valor de desaceleração do Estocástico. |
| `RsiOverbought` | Nível do RSI considerado sobrecomprado. |
| `RsiOversold` | Nível do RSI considerado sobrevendido. |
| `StochOverbought` | Nível do Estocástico considerado sobrecomprado. |
| `StochOversold` | Nível do Estocástico considerado sobrevendido. |
| `TakeProfit` | Distância do take profit em pontos de preço. |
| `TrailingStop` | Distância do trailing stop em pontos de preço. |
| `CandleType` | Tipo e período de velas para analisar. |

## Notas

A estratégia processa apenas velas finalizadas e ignora qualquer sinal até que todos os indicadores estejam completamente formados.
Destina-se a ser um exemplo educativo e deve ser testada cuidadosamente antes de operar ao vivo.
