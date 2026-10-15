# Estratégia de Rompimento Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento usando Canais Donchian com filtros de volatilidade e volume.

A estratégia compra quando o preço fecha acima do canal Donchian superior e a tendência é confirmada por uma EMA e RSI acima de 50. Posições vendidas são abertas em rompimentos abaixo do canal inferior. As posições são fechadas em um sinal Donchian oposto ou quando um stop baseado em ATR é acionado.

## Detalhes

- **Critérios de entrada**: Rompimento do canal Donchian com filtros de EMA, RSI, volatilidade e volume.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Rompimento oposto ou stop ATR.
- **Stops**: Baseado em ATR.
- **Valores padrão**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Donchian, ATR, EMA, RSI, Volume
  - Stops: Stop ATR
  - Complexidade: Intermediário
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
