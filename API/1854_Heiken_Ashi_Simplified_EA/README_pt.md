# Estratégia Heiken Ashi Simplified EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema baseado em padrões construído sobre candles Heikin Ashi. A estratégia observa uma sequência de aberturas e fechamentos anteriores de Heikin Ashi. Quando três fechamentos consecutivos sobem (ou caem) acima de suas respectivas aberturas enquanto as aberturas formam uma retração desacelerada, o próximo candle pode desencadear uma operação de rompimento assim que o preço se afasta da última abertura de Heikin Ashi por uma distância mínima. O algoritmo escala posições até um limite definido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Três fechamentos HA anteriores estão acima de aberturas anteriores e as aberturas formam uma série decrescente com diferenças encolhendo.
  - **Vendido**: Três fechamentos HA anteriores estão abaixo de aberturas anteriores e as aberturas formam uma série crescente com diferenças se expandindo.
- **Comprado/Vendido**: Ambas as direções
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `CandleType` = 1 hora
  - `MaxPositions` = 3
  - `DistancePoints` = 300
  - `Volume` = 1
- **Filtros**:
  - Categoria: Rompimento de padrão
  - Direção: Ambos
  - Indicadores: Heikin Ashi
  - Stops: Não
  - Complexidade: Moderado
  - Período: Por hora
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
