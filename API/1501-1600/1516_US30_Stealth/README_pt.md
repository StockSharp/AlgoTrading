# Estratégia US30 Stealth
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de price action para US30 usando inclinação da média móvel, padrões envolventes, volume e filtro de sessão.
O tamanho da posição é calculado a partir do risco por operação, com stop-loss e take-profit baseados no intervalo da vela.

## Detalhes

- **Critérios de entrada**: Direção de tendência, três máximas decrescentes ou mínimas crescentes, padrão envolvente, filtro de volume e tempo.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Take-profit ou stop-loss
- **Stops**: Fixo
- **Valores padrão**:
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
