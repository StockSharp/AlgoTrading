# Estratégia Two X SPY TIPS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aloca capital no ativo negociado quando tanto o S&P 500 quanto os preços TIPS estão acima de suas médias móveis de 200 períodos na virada de um novo mês.

## Detalhes

- **Critérios de entrada**: S&P 500 e TIPS acima de sua SMA em um novo mês.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Sem saídas.
- **Stops**: Não.
- **Valores padrão**:
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
