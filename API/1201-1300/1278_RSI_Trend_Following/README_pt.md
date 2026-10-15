# Estratégia de Seguidor de Tendência RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Seguidor de Tendência RSI entra comprado quando o momentum é confirmado por RSI, Estocástico, MACD e o preço permanece acima de uma EMA de longo prazo. Um stop de rastreamento é ativado após um movimento favorável baseado em ATR e segue uma EMA mais curta.

As posições são encerradas quando o preço cai abaixo da EMA de rastreamento ou atinge o stop-loss baseado em ATR.

## Detalhes

- **Critérios de entrada**: `K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Preço abaixo da EMA de rastreamento ou stop-loss
- **Stops**: Sim, baseado em ATR
- **Valores padrão**:
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
