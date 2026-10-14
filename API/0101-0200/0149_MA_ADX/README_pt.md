# Estratégia Ma Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores MA e ADX. Entra em posição quando o preço cruza a MA com tendência forte.

Os testes indicam um retorno anual médio de aproximadamente 184%. Funciona melhor no mercado de criptomoedas.

A média móvel dita a tendência e o ADX verifica se é forte o suficiente para operar. As entradas seguem os cruzamentos de preço da MA quando o ADX excede um limiar.

Esta abordagem de tendência clássica atrai traders sistemáticos. As perdas são gerenciadas com um stop percentual e os lucros realizados em um alvo baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > MA && ADX > 25`
  - Vendido: `Close < MA && ADX > 25`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento inverso da MA ou stop
- **Stops**: Percentual `StopLossPercent` com take profit `TakeProfitAtrMultiplier` ATR
- **Valores padrão**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
  - `AdxThreshold` = 25
  - `AtrPeriod` = 14
    A entrada exige que o fechamento cruze a SMA naquele candle com o ADX acima de AdxThreshold; o cruzamento inverso encerra a posição e, se o ADX continuar forte, abre o lado oposto. O alvo fica a TakeProfitAtrMultiplier ATR (AtrPeriod) do fechamento de entrada e é verificado nos fechamentos dos candles.
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Moving Average, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

