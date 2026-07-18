# Estratégia de Tendência de Volume RSI YinYang
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Tendência de Volume RSI YinYang usa zonas de preço ponderadas por volume e um filtro RSI para detectar reversões de tendência. A estratégia compra quando o preço sai da zona inferior e vende quando sai da zona superior. Níveis opcionais de stop-loss e take-profit são baseados em zonas dinâmicas.

## Detalhes

- **Critérios de entrada**: O preço cruza para fora das zonas de compra calculadas com opções de reinício de disponibilidade.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço alcança a zona oposta ou aciona o stop-loss/take-profit opcional.
- **Stops**: Opcional.
- **Valores padrão**:
  - `TrendLength` = 80
  - `UseTakeProfit` = true
  - `UseStopLoss` = true
  - `StopLossMultiplier` = 0.1
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: VWMA, EMA, RSI
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
