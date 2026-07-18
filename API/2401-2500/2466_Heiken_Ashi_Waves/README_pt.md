# Estratégia de Ondas Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina velas Heikin-Ashi com um filtro de onda de média móvel dupla. O cruzamento da SMA rápida (2) acima da SMA lenta (30) sinaliza possíveis mudanças de onda e é confirmado pela direção da vela Heikin-Ashi atual.

## Detalhes

- **Critérios de entrada**:
  - Comprado: vela Heikin-Ashi de alta e SMA rápida cruzando acima da SMA lenta
  - Vendido: vela Heikin-Ashi de baixa e SMA rápida cruzando abaixo da SMA lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Cruzamento oposto
  - Trailing stop loss
- **Stops**: Trailing stop em pontos via `StopLoss`
- **Valores padrão**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, SMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
