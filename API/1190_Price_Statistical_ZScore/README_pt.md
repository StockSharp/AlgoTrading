# Estratégia de Z-Score Estatístico de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza cruzamento de Z-Score suavizado com um filtro de momentum de velas.

Compra quando o Z-Score de curto prazo sobe acima do Z-Score de longo prazo e fecha quando cai abaixo. A estratégia ignora sinais após vários sinais idênticos consecutivos e evita entradas após três velas de alta.

## Detalhes

- **Critérios de entrada**: Z-Score de curto prazo acima do longo prazo, sem sequência de alta anterior de 3 barras, intervalo entre sinais.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Z-Score de curto prazo abaixo do longo prazo, sem sequência de baixa anterior de 3 barras, intervalo entre sinais.
- **Stops**: Não.
- **Valores padrão**:
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
