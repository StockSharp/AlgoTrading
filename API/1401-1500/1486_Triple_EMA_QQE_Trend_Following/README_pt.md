# Estratégia de Seguimento de Tendência Triple EMA + QQE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de tendência que combina duas linhas TEMA com um filtro QQE.
Abre posições compradas quando o preço está acima de ambas as linhas TEMA e o QQE dá um sinal de alta.
Posições vendidas são abertas em condições opostas.
Um stop de rastreamento em pontos protege as operações abertas.

## Detalhes

- **Critérios de entrada**: Alinhamento de TEMA com cruzamento de QQE.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop de rastreamento.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, QQE
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
