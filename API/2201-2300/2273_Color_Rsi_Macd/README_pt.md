# Estratégia Color RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera sinais do indicador MACD que pode ser analisado em quatro modos diferentes:

- **Breakdown** – operar quando o histograma do MACD cruza a linha zero.
- **MACD Twist** – operar quando a linha MACD muda de direção.
- **Signal Twist** – operar quando a linha de sinal muda de direção.
- **MACD Disposition** – operar nos cruzamentos entre a linha MACD e a linha de sinal.

Cada modo pode abrir ou fechar posições compradas e vendidas de forma independente usando os sinalizadores correspondentes.

Não são usados níveis de stop loss ou take profit por padrão.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 4 horas
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
