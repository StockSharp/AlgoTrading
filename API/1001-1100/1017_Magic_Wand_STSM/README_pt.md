# Estratégia Magic Wand STSM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de seguimento de tendência que usa o indicador Supertrend com filtro SMA de 200 períodos. Opera na direção do Supertrend e usa a linha como stop, visando um take profit com relação risco/recompensa configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Supertrend abaixo do preço e fechamento acima da SMA200.
  - **Vendido**: Supertrend acima do preço e fechamento abaixo da SMA200.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Take profit em `entry ± (entry - Supertrend) * RiskReward`.
  - Stop loss no Supertrend.
- **Stops**: Sim.
- **Valores padrão**:
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
