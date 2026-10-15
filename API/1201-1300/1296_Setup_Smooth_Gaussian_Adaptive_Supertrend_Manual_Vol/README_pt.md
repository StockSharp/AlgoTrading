# Configuração: Gaussian Suavizado + Adaptive Supertrend (Vol Manual) — Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado quando o fechamento está acima de uma média móvel duplamente suavizada (tendência "Gaussian").
Sai quando o preço fecha abaixo da linha de tendência. Um filtro de volatilidade manual simples pode restringir as entradas.

## Detalhes

- **Critérios de entrada**: Fechamento acima da linha de tendência e (filtro de volatilidade desativado ou volatilidade é 2 ou 3).
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento abaixo da linha de tendência.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
