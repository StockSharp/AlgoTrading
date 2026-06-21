# Estratégia de Reversão por Rebote em Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que captura reversões quando o preço rebate nas Bollinger Bands com confirmação de MACD e volume. O sistema limita as entradas a cinco operações por dia e aplica stop loss e take profit de percentual fixo.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - Vendido: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **Comprado/Vendido**: Ambos
- **Stops**: Take profit e stop loss em percentual
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Bollinger Bands, MACD, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
