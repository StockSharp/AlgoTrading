# Estratégia de Coeficiente de Correlação de Postos de Spearman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de trading em pares mede a correlação de postos de Spearman entre dois títulos. Quando a correlação excede um limiar positivo, a estratégia fica vendida no primeiro título e comprada no segundo. Quando cai abaixo do limiar negativo, assume a posição oposta. As posições são fechadas quando a correlação retorna para perto de zero.

## Detalhes

- **Critérios de entrada:**
  - **Comprado primeiro / Vendido segundo**: correlação < -Threshold.
  - **Vendido primeiro / Comprado segundo**: correlação > Threshold.
- **Comprado/Vendido**: Trading em pares.
- **Critérios de saída:**
  - Valor absoluto da correlação < Threshold / 2.
- **Stops**: Não.
- **Valores padrão:**
  - `CorrelationPeriod` = 10
  - `Threshold` = 0.8
- **Filtros:**
  - Categoria: Correlação
  - Direção: Ambos
  - Indicadores: Spearman Rank Correlation
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
