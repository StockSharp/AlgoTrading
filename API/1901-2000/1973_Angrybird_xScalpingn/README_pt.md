# Estratégia Angrybird xScalpingn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Angrybird xScalpingn é uma estratégia de scalping no estilo martingale. Ela abre uma operação inicial com base na direção de preço de curto prazo e um filtro RSI. Quando o preço se move contra a posição aberta por um passo dinâmico derivado do range recente, a estratégia adiciona outra operação com volume multiplicado por um fator. Todas as posições são fechadas quando o CCI mostra um forte movimento contrário ou quando o stop-loss ou take-profit é atingido.

## Detalhes

- **Critérios de entrada**: A operação inicial segue a direção de fechamento recente com um filtro RSI. Operações adicionais são abertas quando o preço se move contra a posição pelo passo calculado.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Reversão do CCI ou stop-loss/take-profit protetor.
- **Stops**: Sim.
- **Valores padrão**:
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Grid
  - Direção: Ambos
  - Indicadores: RSI, CCI
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
