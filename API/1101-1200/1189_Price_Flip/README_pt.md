# Estratégia de Price Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Price Flip espelha o preço em torno de máximas e mínimas recentes e opera cruzamentos de médias móveis quando o fechamento anterior está no lado oposto desse preço invertido. Um filtro de tendência baseado na média móvel lenta pode ser aplicado.

## Detalhes

- **Critérios de entrada**:
  - O fechamento anterior está acima do preço invertido.
  - A MA rápida cruza acima da MA lenta.
  - Opcional: o preço está acima da MA lenta quando o filtro de tendência está habilitado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto aciona uma reversão.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, Highest/Lowest
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
