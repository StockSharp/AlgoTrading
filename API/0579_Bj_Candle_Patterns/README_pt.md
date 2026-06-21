# Estratégia de Padrões de Velas Bj
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca os padrões de candles Dragonfly Doji e Gravestone Doji. Um Dragonfly Doji com longa sombra inferior pode sinalizar reversão altista, enquanto um Gravestone Doji com longa sombra superior pode indicar reversão baixista. A estratégia compra após um Dragonfly Doji e vende após um Gravestone Doji.

## Detalhes

- **Critérios de entrada**: Dragonfly Doji → comprado; Gravestone Doji → vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou discricionário.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `DojiThreshold` = 0.1
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
