# Estratégia de Configuração de Operações do Ouro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na Média Móvel Adaptativa de Kaufman e SuperTrend.
Vende quando AMA está subindo e SuperTrend muda para tendência de alta.
Compra quando AMA está caindo e SuperTrend muda para tendência de baixa.

## Detalhes

- **Critérios de entrada**: Direção do AMA com mudança do SuperTrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Níveis fixos de alvo e stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: KAMA, SuperTrend
  - Stops: Sim
  - Complexidade: Médio
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
