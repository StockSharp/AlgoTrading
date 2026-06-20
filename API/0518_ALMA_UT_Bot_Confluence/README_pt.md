# Estratégia ALMA & UT Bot Confluence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia ALMA & UT Bot Confluence combina um filtro de média móvel Arnaud Legoux com um stop trailing no estilo UT Bot. Uma posição comprada é aberta quando o preço está acima da EMA de longo prazo e da ALMA, o volume supera sua média, o RSI sinaliza momentum, o ADX confirma a força da tendência, a vela está abaixo da banda superior de Bollinger e o UT Bot gera um sinal de compra. Entradas vendidas ocorrem quando o UT Bot se torna baixista e o preço cruza abaixo da EMA rápida sob os mesmos filtros. As saídas utilizam o stop trailing do UT Bot ou stop loss e take profit fixos baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço > EMA e ALMA, RSI > 30, ADX > 30, preço < banda superior de Bollinger, sinal de compra UT Bot, filtros de volume e ATR, cooldown.
  - Vendido: preço cruza abaixo da EMA rápida com sinal de venda UT Bot e filtros.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop trailing UT Bot ou stop loss/take profit baseado em ATR e saída temporal opcional.
- **Stops**: ATR ou trailing.
- **Valores padrão**:
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **Filtros**:
  - Categoria: Seguidor de tendência com filtro de volatilidade
  - Direção: Comprado/Vendido
  - Indicadores: EMA, ALMA, ADX, RSI, Bollinger Bands, UT Bot
  - Stops: ATR ou trailing
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
