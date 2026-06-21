# Estratégia de Envolvente com Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um filtro SuperTrend com padrões envolventes de alta e de baixa. Uma operação é aberta quando uma vela envolve a barra anterior na direção da tendência predominante. Os níveis de stop e alvo são calculados a partir do intervalo do padrão.

## Detalhes

- **Critérios de entrada**: Padrão envolvente alinhado com a direção do SuperTrend.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Sim, com base nos extremos da vela e no deslocamento ATR.
- **Valores padrão**:
  - `CandleType` = 5 minutos
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: SuperTrend, Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
