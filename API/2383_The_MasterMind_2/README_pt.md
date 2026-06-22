# Estratégia The MasterMind 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia combina o **Stochastic Oscillator** e o **Williams %R** para identificar condições extremas de sobrevenda e sobrecompra.
Uma posição comprada é aberta quando a linha de sinal do Stochastic cai abaixo de **3** e o Williams %R é inferior a **-99.9**.
Uma posição vendida é aberta quando a linha de sinal do Stochastic sobe acima de **97** e o Williams %R é superior a **-0.1**.

O controle de risco inclui um stop loss inicial e take profit, um trailing stop com passo ajustável e um gatilho de break-even opcional que move o stop para o preço de entrada após lucro suficiente.

## Parâmetros

- `LotSize` - volume de negociação em contratos.
- `StochasticPeriod` - período para o Stochastic Oscillator.
- `StochasticK` - suavização da linha %K.
- `StochasticD` - suavização da linha %D (sinal).
- `WilliamsRPeriod` - período para o Williams %R.
- `StopLossPoints` - stop loss inicial em pontos de preço.
- `TakeProfitPoints` - take profit inicial em pontos de preço.
- `TrailingStopPoints` - distância do trailing stop em pontos.
- `TrailingStepPoints` - movimento favorável mínimo antes de atualizar o trailing stop.
- `BreakEvenPoints` - distância em pontos para mover o stop para break-even.
- `CandleType` - tipo e período dos candles usados nos cálculos.

## Lógica de negociação

1. **Sinais de entrada**
   - **Compra** quando sinal Stochastic < 3 e Williams %R < -99.9.
   - **Venda** quando sinal Stochastic > 97 e Williams %R > -0.1.
2. **Sinais de saída**
   - Sinais de entrada opostos fecham posições existentes.
   - Stop loss, take profit, break-even e trailing stop são aplicados em cada candle.

## Notas

- Funciona em qualquer instrumento que suporte os indicadores necessários.
- Projetada para fins educacionais e experimentação adicional.
