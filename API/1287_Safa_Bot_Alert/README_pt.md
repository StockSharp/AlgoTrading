# Estratégia Safa Bot Alert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Safa Bot Alert usa uma SMA curta com um filtro ADX para operar cruzamentos de preço. Entra comprado quando o preço cruza acima da SMA com força de tendência elevada e entra vendido em cruzamentos abaixo. Take profit fixo, stop loss e um trailing stop gerenciam as posições, e todas as operações são encerradas em um horário de sessão especificado.

## Detalhes

- **Critérios de entrada**: O preço cruza a SMA e ADX > `AdxThreshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit, stop loss, trailing stop ou fechamento de sessão.
- **Stops**: Fixo e Trailing.
- **Valores padrão**:
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, ADX
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
