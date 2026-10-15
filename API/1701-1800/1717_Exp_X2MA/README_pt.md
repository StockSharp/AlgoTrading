# Estratégia Exp X2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Exp X2MA opera nos pontos de virada de uma média móvel com duplo suavizamento.
O preço é primeiro suavizado com uma média móvel simples e depois com uma média móvel Jurik.
Quando a linha suavizada forma um mínimo local, a estratégia compra e fecha posições vendidas.
Quando forma um máximo local, a estratégia vende e fecha posições compradas.
Stop loss fixo e take profit opcionais protegem as posições abertas.

## Detalhes
- **Dados**: Velas de preço (padrão 4 horas).
- **Critérios de entrada**:
  - **Comprado**: O valor anterior do X2MA é menor que o mais antigo e o valor atual vira para cima.
  - **Vendido**: O valor anterior do X2MA é maior que o mais antigo e o valor atual vira para baixo.
- **Critérios de saída**: Extremo oposto, stop loss ou take profit.
- **Stops**: Stop loss fixo e take profit em pontos.
- **Valores padrão**:
  - `FirstMaLength` = 12
  - `SecondMaLength` = 5
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
- **Filtros**:
  - Categoria: Reversão de tendência
  - Direção: Comprado e Vendido
  - Indicadores: SMA, JurikMovingAverage
  - Stops: Sim
  - Complexidade: Baixo
  - Nível de risco: Médio
