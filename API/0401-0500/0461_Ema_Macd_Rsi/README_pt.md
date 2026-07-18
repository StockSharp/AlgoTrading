# Estratégia EMA MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina o filtro de tendência com EMA, cruzamentos de MACD e níveis de RSI.

Compra quando a EMA rápida está acima da EMA lenta, MACD cruza acima de sua linha de sinal e RSI está entre RsiBuyLevel e 70. Vende quando a EMA rápida está abaixo da EMA lenta, MACD cruza abaixo de sua linha de sinal e RSI está entre 30 e RsiSellLevel.

## Detalhes

- **Critérios de entrada**: Filtro de tendência com EMA, cruzamento de MACD, nível de RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, MACD, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
