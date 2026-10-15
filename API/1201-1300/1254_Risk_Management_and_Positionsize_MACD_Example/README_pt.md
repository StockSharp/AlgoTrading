# Gestão de Risco e Tamanho de Posição - Exemplo com MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Gestão de Risco e Tamanho de Posição - Exemplo com MACD** demonstra o dimensionamento dinâmico de posições baseado no patrimônio atual. Ela se baseia em cruzamentos de MACD de um período gráfico superior combinados com um filtro de tendência de média móvel.

## Detalhes
- **Critérios de entrada**: A linha MACD cruza acima/abaixo da linha de sinal com confirmação de tendência.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento MACD oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, Moving Average
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
