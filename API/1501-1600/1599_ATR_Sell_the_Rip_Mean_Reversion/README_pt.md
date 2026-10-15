# Estratégia de Reversão à Média ATR Vender o Pico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente vendida que vende quando o preço sobe acima de um limiar ATR suavizado e cobre numa queda abaixo da mínima anterior. Um filtro EMA opcional limita as operações a tendências de baixa.

## Detalhes

- **Critérios de entrada**: Fechamento acima do suavizado (close + ATR * multiplicador)
- **Comprado/Vendido**: Vendido
- **Critérios de saída**: Fechamento abaixo da mínima anterior
- **Stops**: Não
- **Valores padrão**:
  - `AtrPeriod` = 20
  - `AtrMultiplier` = 1.0
  - `SmoothPeriod` = 10
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Vendido
  - Indicadores: ATR, SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
