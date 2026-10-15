# Estratégia Ichimoku Ponderada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina sinais do Ichimoku em uma pontuação ponderada.
Compra quando a pontuação supera o limiar de compra e sai quando a pontuação cai abaixo do limiar de venda.

## Detalhes

- **Critérios de entrada**: pontuação >= BuyThreshold
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: pontuação <= SellThreshold ou abaixo de zero se o limiar estiver desativado
- **Stops**: Não
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: Ichimoku
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
