# Estratégia de Reversão MACD com Confirmação Stochastic 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compra quando a linha MACD cruza acima da linha de sinal com confirmação do oscilador Stochastic diário. A operação é encerrada quando o preço atinge um stop loss baseado em ATR ou cai abaixo de um take profit EMA trailing.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD crosses above Signal && DailyK > DailyD && DailyK < 80`
- **Comprado/Vendido**: Somente comprado
- **Stops**: Stop loss ATR e take profit EMA trailing
- **Valores padrão**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: MACD, Stochastic, ATR, EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
