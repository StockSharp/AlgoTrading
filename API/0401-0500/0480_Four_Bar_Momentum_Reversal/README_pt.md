# Estratégia de Reversão de Momentum de Quatro Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão de Momentum de Quatro Barras entra comprado quando o fechamento esteve abaixo do fechamento de `Lookback` barras atrás por pelo menos `BuyThreshold` velas consecutivas dentro da janela de tempo selecionada. A posição é fechada assim que o preço rompe acima da máxima da vela anterior.

## Detalhes

- **Critérios de entrada**: `BuyThreshold` fechamentos consecutivos abaixo do fechamento de `Lookback` barras atrás dentro da janela de tempo.
- **Critérios de saída**: Preço de fechamento maior que a máxima da vela anterior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
