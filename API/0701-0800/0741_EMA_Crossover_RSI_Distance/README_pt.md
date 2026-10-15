# Estratégia de Cruzamento EMA com RSI e Distância
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa múltiplas EMAs e RSI para gerar sinais comprados e vendidos, verificando a distância entre as EMAs rápidas para confirmar a força da tendência.

## Detalhes

- **Critérios de entrada**:
  - EMA5 acima de EMA13.
  - EMA40 acima de EMA55.
  - RSI acima de 50 e acima de sua SMA.
  - Distância entre EMA5 e EMA13 acima de sua média e a distância EMA40-EMA13 em aumento.
  - Preço de fechamento acima de EMA5.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - O sinal muda para neutro ou direção oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI
  - Stops: Não
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
