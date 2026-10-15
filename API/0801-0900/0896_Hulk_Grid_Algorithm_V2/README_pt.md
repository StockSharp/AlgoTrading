# Estratégia Hulk Grid Algorithm V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de grid que coloca dez ordens limitadas de compra escalonadas em torno de um preço médio definido pelo usuário. As ordens aumentam de tamanho conforme se aproximam do nível médio. A estratégia fecha todas as posições e cancela as ordens restantes quando o preço atinge um stop-loss abaixo do grid mais baixo ou um take-profit acima do grid superior.

## Detalhes

- **Critérios de entrada**: Grid de dez ordens limitadas de compra do nível mais baixo ao mais alto.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss abaixo do grid mais baixo ou take-profit acima do grid superior.
- **Stops**: Stop-loss e take-profit baseados em percentual.
- **Valores padrão**:
  - `MidPrice` = 0
  - `StopLossPercent` = 2.0
  - `TakeProfitPercent` = 2.0
  - `GridStep` = 200
  - `Lot` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Grid
  - Direção: Long
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
