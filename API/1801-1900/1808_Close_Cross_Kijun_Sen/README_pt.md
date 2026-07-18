# Estratégia de Fechamento por Cruzamento da Kijun-Sen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia funciona como uma ferramenta de gerenciamento de operações. Ela fecha posições existentes quando o preço de fechamento cruza a linha Kijun-sen do indicador Ichimoku.

Durante a execução, a estratégia se inscreve em velas e calcula o valor do Kijun-sen. Quando uma posição comprada está presente e o preço cai abaixo da linha Kijun por um deslocamento configurável, a posição é fechada. Quando uma posição vendida está aberta e o preço sobe acima da linha, a posição também é fechada. A estratégia não abre novas operações.

## Detalhes

- **Critérios de entrada**: A estratégia não abre novas operações; apenas gerencia posições existentes.
- **Comprado/Vendido**: Ambos (fechamento).
- **Critérios de saída**: Preço de fechamento cruzando a linha Kijun-sen pelo deslocamento especificado.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `KijunPeriod` = 50
  - `PointsToCross` = 0
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Gerenciamento de operações
  - Direção: Ambos
  - Indicadores: Ichimoku
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
