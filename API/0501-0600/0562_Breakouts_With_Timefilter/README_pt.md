# Estratégia de Rompimentos com Filtro de Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que entra quando o preço cruza acima de máximas recentes ou abaixo de mínimas recentes dentro de uma sessão de negociação especificada. Um filtro de média móvel opcional confirma a direção. O stop-loss pode ser baseado em ATR, extremos de candle ou pontos fixos com um alvo de risco-recompensa configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento > máxima mais alta em `Length` e dentro da janela de tempo; opcionalmente Fechamento > MA.
  - **Vendido**: Fechamento < mínima mais baixa em `Length` e dentro da janela de tempo; opcionalmente Fechamento < MA.
- **Comprado/Vendido**: Ambos
- **Stops**: ATR, baseado em candle ou pontos fixos com alvo de risco-recompensa
- **Valores padrão**:
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
