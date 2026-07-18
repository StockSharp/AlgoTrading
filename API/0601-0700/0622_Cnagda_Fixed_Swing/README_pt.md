# Estratégia Cnagda Fixed Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia que utiliza velas Heikin Ashi com dois modos:
- **RSI**: entradas quando a EMA curta do RSI cruza a EMA longa com volume alto.
- **Scalp**: entradas baseadas em cruzamentos de EMA e WMA do fechamento de Heikin Ashi.

O stop loss é colocado no swing alto ou baixo recente e o take profit usa um múltiplo fixo de risco/retorno.

## Parâmetros
- Tipo de candle
- Lógica de operação
- Retrocesso de swing
- Risco/retorno
