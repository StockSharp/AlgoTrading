# Estratégia de Scalping EMA RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping em velas de 30 minutos que combina cruzamento de EMA rápida/lenta, EMA de tendência, filtros RSI e MACD com uma condição de volume. O stop-loss é baseado no ATR e o take profit usa uma relação risco-recompensa fixa.

## Detalhes

- **Critérios de entrada**: EMA rápida cruzando a EMA lenta na direção da tendência, RSI dentro dos limites, confirmação do MACD e volume alto.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Stop oposto ou alvo atingido.
- **Stops**: Stop-loss baseado em ATR e take profit por risco-recompensa.
- **Valores padrão**:
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: EMA, RSI, MACD, ATR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (30m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
