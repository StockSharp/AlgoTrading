# Estratégia Hull Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Hull Candles é uma estratégia simples de seguidor de tendência que usa uma Hull Moving Average do preço médio (OHLC4). Quando o HMA sobe e o fechamento está acima de sua SMA, abre posições compradas; quando o HMA cai e o fechamento está abaixo de sua SMA, abre posições vendidas.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: HMA sobe e fechamento > SMA.
  - **Vendido**: HMA cai e fechamento < SMA.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BodyLength` = 10
  - `SmaLength` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: HMA, SMA
  - Complexidade: Baixo
  - Nível de risco: Alto
