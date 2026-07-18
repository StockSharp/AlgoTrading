# Estratégia Agressiva de Alta IV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Agressiva de Alta IV combina cruzamentos de EMA com um filtro de volatilidade ATR. As operações são abertas apenas quando a volatilidade supera sua média em um desvio padrão e fechadas com alvos baseados em ATR.

Os testes indicam retornos sólidos em mercados de alta volatilidade.

A estratégia entra em cruzamentos de EMA durante períodos de volatilidade elevada, buscando ganhos rápidos com controles de risco predefinidos.

As posições são fechadas usando níveis de stop-loss e take-profit baseados em ATR.

## Detalhes

- **Critérios de entrada**: EMA rápida cruza EMA lenta com ATR acima de sua média mais o desvio padrão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit baseado em ATR atingido.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 30
  - `AtrLength` = 14
  - `AtrMeanLength` = 20
  - `AtrStdLength` = 20
  - `RiskFactor` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
