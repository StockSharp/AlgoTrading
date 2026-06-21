# Estratégia RSI & MA Ponderado Invertido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza o Índice de Força Relativa e uma média móvel ponderada de forma inversa com um filtro de taxa de variação. Posições compradas são abertas quando o RSI supera o limiar e o ROC da MA está abaixo do nível definido, enquanto posições vendidas são abertas nas condições opostas. O sistema aplica um stop trailing baseado em ATR e dimensionamento de posição por razão fixa.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI >= RsiLongSignal` e `MA ROC <= RocMaLongSignal`
  - **Vendido**: `RSI <= RsiShortSignal` e `MA ROC >= RocMaShortSignal`
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto, stop loss ou stop trailing.
- **Stops**: Sim, stop trailing ATR e percentual máximo de perda.
- **Valores padrão**:
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RSI, Moving Average, ATR
  - Stops: Sim
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
