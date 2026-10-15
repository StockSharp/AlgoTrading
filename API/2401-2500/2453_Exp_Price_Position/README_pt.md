# Exp Posição de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Exp Price Position** adapta o consultor especializado original do MetaTrader que combina a localização do preço e um filtro de tendência escalonada.
Ela avalia a relação entre duas médias móveis medianas para localizar o último nível de oscilação e, em seguida, verifica um par de médias móveis suavizadas rápida e lenta para determinar a direção da tendência.
As ordens são abertas apenas quando tanto a posição do preço quanto a tendência escalonada concordam com a estrutura da vela atual.

A estratégia é projetada para mercados onde as mudanças de tendência ocorrem após o preço recuar para um nível mediano dinâmico. Um stop dinâmico e uma relação de take-profit são aplicados para gerenciar o risco.

## Detalhes

- **Critérios de entrada**: Preço acima do último nível de oscilação com tendência escalonada de alta para operações compradas; abaixo com tendência escalonada de baixa para operações vendidas.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop de proteção.
- **Stops**: Sim, via trailing stop com relação de take-profit.
- **Valores padrão**:
  - `FastPeriod` = 2
  - `SlowPeriod` = 30
  - `MedianFastPeriod` = 26
  - `MedianSlowPeriod` = 20
  - `TpSlRatio` = 3m
  - `TrailingStopPips` = 10m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Smoothed Moving Average, Simple Moving Average
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
