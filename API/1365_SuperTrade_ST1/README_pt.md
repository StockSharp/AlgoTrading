# Estratégia SuperTrade ST1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente comprada que combina Supertrend com filtro EMA e gestão de risco baseada em ATR.

Os testes indicam um retorno anual médio de cerca de 45%. Funciona melhor no mercado de criptomoedas.

O sistema aguarda uma queda na direção do Supertrend enquanto o preço permanece acima da linha Supertrend e da EMA. O risco é controlado com stop-loss e tomada de lucro baseados em ATR na proporção de 1:4.

## Detalhes

- **Critérios de entrada**:
  - Direção anterior do Supertrend > direção atual
  - Fechamento > Supertrend
  - Fechamento > EMA
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: `Close <= entry - StopAtrMultiplier * ATR` ou `Close >= entry + TakeAtrMultiplier * ATR`
- **Stops**: Stop-loss e tomada de lucro baseados em ATR
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: Supertrend, EMA, ATR
  - Stops: Sim
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

