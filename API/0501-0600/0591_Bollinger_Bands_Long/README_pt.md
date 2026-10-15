# Estratégia de Bollinger Bands Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o preço fecha abaixo da banda inferior de Bollinger e o RSI está sobrevendido. Sai da posição comprada assim que o preço retorna à banda do meio.

## Detalhes

- **Critérios de entrada**:
  - O preço fecha abaixo da banda inferior de Bollinger.
  - RSI abaixo do nível de sobrevenda.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço fecha na ou acima da banda do meio de Bollinger.
- **Stops**: Não.
- **Valores padrão**:
  - `BbLength` = 10
  - `BbDeviation` = 2
  - `RsiLength` = 14
  - `RsiOversold` = 30
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Bollinger Bands, RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
