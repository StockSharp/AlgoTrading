# Estratégia HMA Plus Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de média móvel Hull adaptativa que ajusta seu período com base na volatilidade ou no volume. Abre posições compradas ou vendidas quando a inclinação do HMA aponta na direção da tendência durante condições de mercado ativas.

## Detalhes

- **Critérios de entrada**: Sinais baseados em HMA adaptativa, ATR ou volume.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, ATR, Volume
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

