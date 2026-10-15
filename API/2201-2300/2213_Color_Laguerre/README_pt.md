# Estratégia Color Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência baseada no oscilador Color Laguerre.

O oscilador Color Laguerre suaviza a série de preços usando um filtro de Laguerre e marca a direção da tendência por mudanças de cor. A estratégia compra quando o oscilador fica em alta e vende quando fica em baixa. Níveis extremos podem forçar saídas se o momentum do preço diminuir.

## Detalhes

- **Critérios de entrada**: Oscilador cruzando o nível médio.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Oscilador
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

