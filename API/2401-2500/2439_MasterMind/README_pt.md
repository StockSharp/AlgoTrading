# A Estratégia MasterMind
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o oscilador Stochastic e Williams %R para capturar condições extremas de sobrecompra e sobrevenda.

## Visão geral
A estratégia monitoriza dois indicadores de momentum:
- **Stochastic Oscillator** com comprimento base 100 e suavização 3/3.
- **Williams %R** com comprimento 100.

Uma posição comprada é aberta quando o valor %D do Stochastic cai abaixo de 3 enquanto Williams %R está abaixo de -99.9, indicando um mercado sobrevendido.
Uma posição vendida é aberta quando o %D do Stochastic sobe acima de 97 e Williams %R sobe acima de -0.1, sinalizando um mercado sobrecomprado.

Após entrar numa operação, o algoritmo gere o risco através de stop loss, take profit, trailing stop e movimento opcional de break-even.

## Parâmetros
- `StochasticLength` – período para os cálculos do Stochastic e Williams %R.
- `StopLoss` – distância do preço de entrada para o stop loss em pontos.
- `TakeProfit` – distância do take profit em pontos.
- `TrailingStop` – distância de ativação do trailing em pontos.
- `TrailingStep` – passo do trailing stop em pontos.
- `BreakEven` – lucro em pontos no qual o stop é movido para a entrada.
- `CandleType` – período temporal de velas para os cálculos da estratégia.

## Indicadores
- `StochasticOscillator`
- `WilliamsR`

## Regras de trading
1. **Comprar** quando `%D < 3` e `Williams %R < -99.9`.  
2. **Vender** quando `%D > 97` e `Williams %R > -0.1`.  
3. Após a entrada, aplicar stop loss e take profit.  
4. Mover o stop para break-even quando o preço avança `BreakEven`.  
5. Ativar o trailing stop quando o preço se move `TrailingStop`, deslocando `TrailingStep`.

## Notas
A estratégia usa a API de alto nível do StockSharp e destina-se a ser um exemplo educativo.
