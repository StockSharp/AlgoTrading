# Estratégia IU Maior que o Range
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que abre operações quando o corpo da vela é maior que o range anterior das velas recentes.

O sistema compara o corpo da vela atual com o range entre o open/close mais alto e o open/close mais baixo durante um período de lookback configurável. Se o corpo exceder o range anterior, entra na direção da vela e gerencia o risco por métodos de stop configuráveis.

## Detalhes

- **Critérios de entrada**: Corpo da vela maior que o range anterior; direção baseada no corpo da vela.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Vela anterior, ATR ou níveis de swing.
- **Valores padrão**:
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
