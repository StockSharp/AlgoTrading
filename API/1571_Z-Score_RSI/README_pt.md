# Estratégia Z-Score RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Z-Score RSI calcula o RSI sobre o z-score do preço e usa uma EMA do RSI para sinais. Uma posição comprada é aberta quando o RSI cruza acima de sua EMA e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**: RSI do z-score cruza sua EMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation, RSI, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
