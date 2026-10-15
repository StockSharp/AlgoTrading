# Estratégia John Bob Trading Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que combina níveis de máxima/mínima de 50 barras com detecção simples de fair value gaps. Abre cinco ordens escalonadas com stop-loss baseado em ATR e múltiplos níveis de take profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço cruza acima da mínima de 50 barras ou aparece um fair value gap de alta
  - Vendido: o preço cruza abaixo da máxima de 50 barras ou aparece um fair value gap de baixa
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço atinge um dos cinco níveis de take profit
  - O preço atinge o stop-loss baseado em ATR
- **Stops**: Multiplicador ATR
- **Valores padrão**:
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
