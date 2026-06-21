# Estratégia de Comprado Pós-Abertura com ATR Stop Loss e Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra em uma posição comprada durante a abertura do mercado após um rompimento de resistência enquanto o preço permanece próximo da banda média de Bollinger. Utiliza filtros EMA, RSI, ADX e ATR e sai por meio de stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Rompimento acima da resistência recente durante a abertura do mercado, preço próximo à banda média de Bollinger, RSI acima do limite, ADX acima do limite, tendência de curto prazo de alta e sem retração.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Stop loss ou take profit baseado em ATR atingido.
- **Stops**:
  - Stop loss e take profit baseados em ATR.
- **Valores padrão**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Long
  - Indicadores: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
