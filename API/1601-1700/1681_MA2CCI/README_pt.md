# Estratégia MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o cruzamento de uma Média Móvel Simples (SMA) rápida e lenta com o Commodity Channel Index (CCI) como filtro de confirmação. Uma posição é aberta somente quando tanto as médias móveis quanto o CCI cruzam seus níveis na mesma direção. O Average True Range (ATR) define a distância inicial do stop-loss.

O sistema pode operar em ambas as direções. Não há take-profit; as posições são fechadas em um sinal oposto ou quando o stop-loss baseado em ATR é acionado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápida cruza acima da SMA lenta **e** CCI cruza acima de 0.
  - **Vendido**: SMA rápida cruza abaixo da SMA lenta **e** CCI cruza abaixo de 0.
- **Critérios de saída**:
  - Cruzamento inverso de SMA.
  - Stop-loss baseado em ATR.
- **Indicadores**: SMA, CCI, ATR.
- **Período**: Configurável via `CandleType`.
- **Parâmetros padrão**:
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **Comprado/Vendido**: Ambos.
- **Stops**: Sim, stop dinâmico usando ATR.
