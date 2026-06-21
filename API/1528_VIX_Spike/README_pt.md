# Estratégia de Pico VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra quando o índice VIX sobe acima de sua média móvel por um múltiplo do desvio padrão e fecha após um número fixo de barras.

## Detalhes

- **Critérios de entrada**: VIX > média + StdDevMultiplier * desvio padrão.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Sair após `ExitPeriods` barras.
- **Stops**: Sim.
- **Valores padrão**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Somente comprado
  - Indicadores: SMA, StdDev
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
