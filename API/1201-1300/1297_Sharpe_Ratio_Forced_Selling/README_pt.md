# Venda Forçada por Sharpe Ratio — Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Venda Forçada por Sharpe Ratio entra comprado quando o Sharpe Ratio móvel cai abaixo de um limiar negativo e sai quando ele sobe acima de um limiar positivo ou o período de manutenção ultrapassa um limite. Os retornos podem ser calculados usando variações logarítmicas ou simples e ajustados por uma taxa livre de risco.

## Detalhes

- **Critérios de entrada**: Sharpe Ratio abaixo de `EntrySharpeThreshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Sharpe Ratio acima de `ExitSharpeThreshold` ou período de manutenção ultrapassa `MaxHoldingDays`.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Sharpe Ratio
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
