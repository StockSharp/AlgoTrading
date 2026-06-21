# Estratégia Supertrend AT v1.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia baseada no Supertrend que abre uma posição comprada quando o Supertrend muda de baixa para alta e uma posição vendida quando muda de alta para baixa. O tamanho da posição é calculado a partir do risco por operação, e as saídas usam níveis de stop-loss e take-profit derivados do Supertrend anterior.

## Detalhes

- **Critérios de entrada**: Mudança de direção do Supertrend.
- **Comprado/Vendido**: Comprado e Vendido.
- **Critérios de saída**: Alvo ou stop atingido.
- **Stops**: Sim.
- **Valores padrão**:
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: Supertrend
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
