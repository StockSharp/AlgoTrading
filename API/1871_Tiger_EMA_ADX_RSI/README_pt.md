# Estratégia Tiger EMA ADX RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue a tendência usando o cruzamento de duas médias móveis exponenciais (EMA) e filtra as operações com o Índice Direcional Médio (ADX) e o Índice de Força Relativa (RSI). A EMA rápida é comparada com a EMA lenta para determinar a direção da tendência. As operações são permitidas somente quando o ADX excede um limite configurável e o RSI permanece dentro dos limites superior e inferior.

Se nenhuma posição estiver aberta e todas as condições forem satisfeitas, a estratégia entra na direção da tendência. Cada entrada define distâncias fixas de take profit e stop loss a partir do preço de entrada. A posição é fechada quando qualquer nível é atingido. O volume da ordem é definido pela propriedade `Volume` da estratégia.

## Parâmetros

- **Fast EMA** – período da média móvel exponencial rápida.
- **Slow EMA** – período da média móvel exponencial lenta.
- **ADX Period** – período de cálculo do ADX.
- **ADX Threshold** – valor mínimo de ADX necessário para operar.
- **RSI Period** – período de cálculo do RSI.
- **RSI Upper** – valor máximo de RSI para entradas compradas.
- **RSI Lower** – valor mínimo de RSI para entradas vendidas.
- **Take Profit** – distância do preço de entrada ao take profit em pontos de preço.
- **Stop Loss** – distância do preço de entrada ao stop loss em pontos de preço.
- **Candle Type** – período ou outro tipo de vela utilizado para cálculos de indicadores.
