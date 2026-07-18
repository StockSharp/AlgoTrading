# Estratégia de Scalping AUD/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia faz scalping em AUD/USD em períodos curtos usando uma combinação de filtro de tendência EMA, Bollinger Bands e RSI. As EMAs rápida e lenta definem a direção da tendência. Operações compradas são abertas em tendências de alta quando o preço toca a banda inferior de Bollinger e o RSI está acima do limiar de sobrevenda. Posições vendidas são tomadas em tendências de baixa quando o preço atinge a banda superior e o RSI está abaixo do nível de sobrecompra. Take profit e stop loss fixos gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápido acima do EMA lento, preço na ou abaixo da banda inferior de Bollinger, RSI acima do nível de sobrevenda.
  - **Vendido**: EMA rápido abaixo do EMA lento, preço na ou acima da banda superior de Bollinger, RSI abaixo do nível de sobrecompra.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Stop loss e take profit fixos.
- **Valores padrão**:
  - `EmaShort` = 13
  - `EmaLong` = 26
  - `RsiPeriod` = 4
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `TakeProfit` = 0.0005
  - `StopLoss` = 0.0004
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: EMA, Bollinger Bands, RSI
  - Stops: Fixo
  - Complexidade: Baixo
  - Período: 1 minuto
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
