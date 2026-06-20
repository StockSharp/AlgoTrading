# Scalping de Grade Fractal Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Scalping de Grade Fractal Adaptativa coloca ordens limitadas em torno de pivôs fractais recentes usando o ATR para a distância. A tendência é definida por uma média móvel simples. Quando a volatilidade supera um limiar, limites de compra são colocados abaixo das mínimas fractais em tendências de alta e limites de venda acima das máximas fractais em tendências de baixa. As saídas ocorrem no nível de grade oposto ou em um stop trailing baseado em ATR.

## Detalhes

- **Critérios de entrada**: ATR acima do limiar com o preço em relação à SMA; limite de compra na mínima fractal menos o multiplicador ATR ou limite de venda na máxima fractal mais o multiplicador ATR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Nível de grade oposto ou stop baseado em fractais.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrLength` = 14
  - `SmaLength` = 50
  - `GridMultiplierHigh` = 2.0m
  - `GridMultiplierLow` = 0.5m
  - `TrailStopMultiplier` = 0.5m
  - `VolatilityThreshold` = 1.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: Fractal, ATR, SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
