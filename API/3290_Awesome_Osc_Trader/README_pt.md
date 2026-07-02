# Estratégia Awesome Osc Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o expert do MetaTrader "Awesome Osc Trader" combinando largura da Bollinger Band, um filtro stochastic e uma verificação normalizada de momentum Awesome Oscillator. Operações compradas são abertas quando o oscilador sobe a partir de um extremo negativo enquanto stochastic sai da área de sobrevenda e a volatilidade de mercado permanece dentro de uma largura de banda configurável. Vendidas exigem as condições espelhadas. Uma janela de negociação configurável limita novas ordens a horas específicas, e posições abertas só podem ser forçadamente fechadas em sinais opostos se o lucro flutuante corresponder ao filtro escolhido.

## Detalhes

- **Critérios de entrada**:
  - O spread da Bollinger Band, convertido para pips, deve permanecer entre `BollingerSpreadLowerLimit` e `BollingerSpreadUpperLimit`.
  - A linha principal stochastic está acima de `StochLower` para compradas ou abaixo de `StochUpper` para vendidas.
  - O Awesome Oscillator normalizado mostrou pelo menos quatro barras consecutivas no lado oposto de zero e está retornando para zero com força acima de `AoStrengthLimit`.
  - A hora atual está dentro da janela de negociação definida por `EntryHour` e `OpenHours`.
- **Comprado/Vendido**: negocia ambas as direções.
- **Critérios de saída**:
  - Saída antecipada opcional quando aparece um sinal oposto ou quando o oscilador cruza zero, controlada por `CloseTrade` e `ProfitTypeClTrd`.
  - Distâncias de stop-loss, take-profit e trailing stop de proteção fornecidas em pips.
- **Stops**: stop fixo, take-profit e trailing stop opcional gerenciados por `StartProtection`.
- **Valores padrão**:
  - `BollingerPeriod` = 20, `BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55, `BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3, `PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8, `StochD` = 3, `StochSlow` = 3
  - `StochLower` = 18, `StochUpper` = 76
  - `EntryHour` = 0, `OpenHours` = 16
  - `Lots` = 0.01, `TakeProfit` = 200, `StopLoss` = 80, `TrailingStop` = 40
  - `CloseTrade` = true, `ProfitTypeClTrd` = 1 (fecha apenas posições lucrativas)
- **Filtros**:
  - Categoria: Momentum com filtro de volatilidade
  - Direção: Comprado e vendido
  - Indicadores: Bollinger Bands, Stochastic Oscillator, Awesome Oscillator
  - Stops: Sim (fixo e trailing)
  - Complexidade: Médio
  - Período: Projetada para H1, mas funciona com qualquer série de candles
  - Sazonalidade: Janela de horas de negociação
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
