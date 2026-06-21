# Estratégia Exchange Price
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compara o preço de fechamento atual com os preços de várias barras atrás durante dois períodos de lookback. Uma posição comprada é aberta quando a variação de curto prazo ultrapassa a variação de longo prazo; uma posição vendida é aberta quando ocorre o cruzamento oposto.

## Detalhes

- **Critérios de entrada**: diferença de preço de curto prazo cruzando acima/abaixo da diferença de longo prazo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `ShortPeriod` = 96
  - `LongPeriod` = 288
  - `CandleType` = candles de 8 horas
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Diferença de preço
  - Stops: Não
  - Complexidade: Básico
  - Período: 8 horas
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
