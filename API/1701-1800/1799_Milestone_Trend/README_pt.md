# Estratégia Milestone Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem em StockSharp do consultor especialista Milestone 22.5. Ela opera retrações dentro de uma tendência combinando duas médias móveis suavizadas com um filtro de volatilidade e de picos. Quando uma vela rompe o extremo da barra anterior e a média rápida confirma o movimento, uma posição é aberta na direção da tendência dominante. O ATR evita operar em mercados calmos e grandes corpos de velas são tratados como picos.

Backtests da versão MQL original mostram bom desempenho nos principais pares de forex. A tradução em C# foca na clareza e usa apenas ordens de mercado para entradas e saídas.

## Detalhes

- **Critérios de entrada**:
  - Força de tendência entre `MinTrend` e `MaxTrend`.
  - A vela rompe a máxima ou mínima anterior e a SMA rápida confirma.
  - ATR acima de `MinRange` e corpo da vela abaixo de `CandleSpike`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O sinal oposto fecha a posição.
- **Stops**: Não implementados; o sinal oposto atua como stop.
- **Valores padrão**:
  - `SlowMaPeriod` = 120
  - `FastMaPeriod` = 30
  - `AtrPeriod` = 14
  - `MinTrend` = 10
  - `MaxTrend` = 100
  - `MinRange` = 5
  - `CandleSpike` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
