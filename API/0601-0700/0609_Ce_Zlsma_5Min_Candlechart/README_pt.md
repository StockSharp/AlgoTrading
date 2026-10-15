# Estratégia CE ZLSMA 5MIN Candlechart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema seguidor de tendência que usa ZLSMA em candles Heikin Ashi com um filtro Chandelier Exit. Compra quando a tendência vira para alta e o candle fecha acima do ZLSMA.

## Detalhes

- **Critérios de entrada**:
  - Comprado: direção vira para cima, fechamento do Heikin Ashi acima do ZLSMA e da abertura
- **Comprado/Vendido**: Comprado
- **Critérios de saída**:
  - Comprado: fechamento abaixo do ZLSMA
- **Stops**: Nenhum
- **Valores padrão**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: ZLSMA, ATR, Heikin Ashi
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
