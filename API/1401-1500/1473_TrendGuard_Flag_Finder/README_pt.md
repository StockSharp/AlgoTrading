# Estratégia TrendGuard Flag Finder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

TrendGuard Flag Finder identifica padrões de bandeira de alta e de baixa confirmados pelo SuperTrend.
Compra quando o preço rompe acima de uma bandeira de alta e vende quando rompe abaixo de uma bandeira de baixa.

## Detalhes

- **Critérios de entrada**: Rompimento de bandeira com confirmação do SuperTrend
- **Comprado/Vendido**: Configurável
- **Critérios de saída**: Rompimento oposto de bandeira
- **Stops**: Não
- **Valores padrão**:
  - `TradingDirection` = Both
  - `SuperTrend Length` = 10
  - `SuperTrend Factor` = 4
  - `MaxFlagDepth` = 5
  - `MinFlagLength` = 3
  - `MaxFlagLength` = 7
  - `MaxFlagRally` = 5
  - `MinBearFlagLength` = 3
  - `MaxBearFlagLength` = 7
  - `PoleMin` = 3
  - `PoleLength` = 7
  - `PoleMinBear` = 3
  - `PoleLengthBear` = 7
- **Filtros**:
  - Categoria: Padrão
  - Direção: Configurável
  - Indicadores: SuperTrend, Lowest, Highest
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
