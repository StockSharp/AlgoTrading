# Estratégia de Cruzamento RSI com Juros Compostos (Mensal)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia investe todo o capital quando o RSI mensal fecha acima da sua SMA e sai quando o RSI cai abaixo da SMA. Os ganhos são adicionados ao capital para capitalização composta.

Os backtests sugerem um retorno anual médio de cerca de 20%. Funciona melhor em ações.

## Detalhes

- **Critérios de entrada**: RSI acima da sua SMA
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: RSI abaixo da sua SMA
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 mês
  - `RsiPeriod` = 14
  - `InitialCapital` = 100000
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: RSI, SMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Mensal
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
