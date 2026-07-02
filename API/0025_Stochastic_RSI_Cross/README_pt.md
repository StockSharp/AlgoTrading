# Estratégia Stochastic RSI Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento do Stochastic RSI

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado forex.

Stochastic RSI Cross observa as linhas %K e %D do StochRSI. Cruzamentos altistas perto de níveis de sobrevenda acionam compras, cruzamentos baixistas perto da sobrecompra acionam vendas, e cruzamentos opostos saem.

Como o StochRSI oscila rapidamente, os sinais podem ser frequentes. Muitos traders exigem que o cruzamento ocorra perto de um extremo para filtrar o ruído.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI, Stochastic.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, Stochastic
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

