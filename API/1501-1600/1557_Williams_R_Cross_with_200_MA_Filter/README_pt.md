# Estratégia de Cruzamento de Williams %R com Filtro de 200 MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera cruzamentos de Williams %R em torno do nível -50 com um filtro de tendência SMA de 200 períodos.
As posições são fechadas com distâncias fixas de alvo de lucro e stop.

## Detalhes

- **Critérios de entrada**: %R cruza os limiares com preço relativo à SMA 200
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: alvo de lucro ou stop
- **Stops**: Sim
- **Valores padrão**:
  - `WrLength` = 14
  - `CrossThreshold` = 10
  - `TakeProfit` = 30
  - `StopLoss` = 20
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: WilliamsR, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
