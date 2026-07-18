# Estratégia Dubic EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na posição do fechamento em relação a médias móveis exponenciais calculadas sobre máximas e mínimas. Evita-se operar durante intervalos estreitos e períodos de baixa volatilidade. As posições são protegidas com stops baseados em ATR, níveis de take-profit e stop trailing opcional do Parabolic SAR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Close > EMA(High) e Close > EMA(Low), filtro de intervalo inativo, volatilidade suficiente.
  - **Vendido**: Close < EMA(High) e Close < EMA(Low), filtro de intervalo inativo, volatilidade suficiente.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Parabolic SAR, stop-loss baseado em ATR/fixo ou take-profit.
- **Stops**: Sim.
- **Filtros**: Filtro de intervalo e volatilidade.
