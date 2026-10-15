# Estratégia NY ORB CP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento do intervalo de abertura de NY com confirmação de reteste. Opera rompimentos do intervalo 9:30-9:45 de NY quando o preço retesta e retoma a direção do rompimento.

## Detalhes

- **Critérios de entrada**:
  - Comprado: O preço retesta a máxima de NY após o rompimento com confirmação de tendência e volume.
  - Vendido: O preço retesta a mínima de NY após o rompimento de baixa com confirmação de tendência e volume.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Alvo de lucro em 0.33 do intervalo * `RiskReward`.
  - Stop loss em 0.33 do intervalo.
- **Stops**: Sim, dinâmicos.
- **Valores padrão**:
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, VWAP, SMA
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
