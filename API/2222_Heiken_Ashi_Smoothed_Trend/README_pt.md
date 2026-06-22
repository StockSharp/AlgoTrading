# Estratégia de Tendência Heiken Ashi Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza candles Heiken-Ashi suavizados por EMA para detectar reversões de tendência. Um candle altista mudando de vermelho para verde abre uma posição comprada e fecha qualquer vendida. Por outro lado, um candle baixista mudando de verde para vermelho abre uma posição vendida e fecha qualquer comprada.

- **Indicadores**: Heikin-Ashi (com suavização EMA)
- **Regras de entrada**:
  - Entrar comprado quando o candle Heikin-Ashi suavizado se torna altista.
  - Entrar vendido quando o candle suavizado se torna baixista.
- **Regras de saída**:
  - Reverter posição no sinal oposto.
- **Parâmetros**:
  - `EmaLength` – período de suavização da EMA.
  - `CandleType` – período dos candles.

O algoritmo recalcula a abertura e o fechamento suavizados para cada candle concluído e altera a posição de acordo.
