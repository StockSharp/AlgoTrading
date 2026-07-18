# Estratégia Hoffman Heiken Bias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Hoffman Heiken Bias combina um grupo de médias móveis com um modelo de volume líquido Heikin Ashi para medir a direção da tendência. Uma posição comprada é aberta quando a SMA rápida sobe acima da EMA rápida enquanto todas as médias de prazo mais longo permanecem abaixo dela e a regressão do volume líquido é positiva. As posições vendidas são ativadas nas condições opostas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `SMA(5) > EMA(18)` && todas as médias mais longas abaixo de `EMA(18)` && regressão de volume líquido > 0.
  - **Vendido**: `SMA(5) < EMA(18)` && todas as médias mais longas acima de `EMA(18)` && regressão de volume líquido < 0.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fast SMA` = 5
  - `Fast EMA` = 18
  - `Net volume length` = 25
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA, ATR, Linear Regression
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
