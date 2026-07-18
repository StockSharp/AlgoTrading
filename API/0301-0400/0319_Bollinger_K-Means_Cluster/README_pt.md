# Estratégia de Bollinger K-Means Cluster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Bollinger K-Means Cluster** é construída em torno do Bollinger K-Means Cluster.

Os sinais são ativados quando o Bollinger confirma mudanças de tendência em dados intradiários (5m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como BollingerLength, BollingerDeviation. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `KMeansHistoryLength = 50`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
