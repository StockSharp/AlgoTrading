# Estratégia Super Simple RSI Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especialista original SSEATwRSI do MetaTrader no StockSharp. Ela monitora candles concluídos e calcula um RSI de 7 períodos sobre o máximo dos candles. Uma negociação é acionada apenas quando o RSI atinge um extremo e as duas barras anteriores formam uma reversão de engolfamento limpa.

Uma configuração comprada requer que o RSI suba acima do limiar de sobrecompra enquanto um candle baixista é completamente engolfado pelo próximo candle altista. Uma configuração vendida espelha essa lógica usando uma leitura de RSI sobrevendida e um padrão de engolfamento altista-a-baixista. O tamanho da posição é fixado pelo parâmetro `Volume`, mas qualquer exposição oposta é fechada antes de abrir uma nova negociação.

Uma vez no mercado, a estratégia continua monitorando o lucro e perda global. Se o PnL flutuante atingir o objetivo de lucro configurado (em moeda da conta) ou cair abaixo da perda permitida, ela fecha toda a posição. Não há trailing stops adicionais; as negociações são gerenciadas exclusivamente pela reversão do padrão e pelos limiares de nível de conta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI nos máximos > `OverboughtLevel` e o último candle envolve uma barra baixista de duas barras atrás enquanto o preço fecha acima da abertura mais antiga.
  - **Vendido**: RSI nos máximos < `OversoldLevel` e o último candle envolve uma barra altista de duas barras atrás enquanto o preço fecha abaixo da abertura mais antiga.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - PnL da conta ≥ `ProfitGoal` → fechar.
  - PnL da conta ≤ `-MaxLoss` → fechar.
  - O sinal oposto compensa automaticamente a posição anterior quando uma nova ordem é colocada.
- **Stops**: Verificações de take-profit e perda máxima baseadas em moeda derivadas do PnL total da estratégia.
- **Filtros**:
  - RSI calculado sobre o máximo do candle para enfatizar movimentos de esgotamento.
  - Confirmação via reversão de engolfamento de duas barras.

## Parâmetros

- `Volume` = 0.1 – Tamanho da ordem em contratos. A exposição existente é compensada antes de abrir uma nova negociação.
- `ProfitGoal` = 190 – Meta de lucro em moeda que força uma posição plana uma vez atingida.
- `MaxLoss` = 10 – Perda máxima permitida em moeda antes de a estratégia fechar todas as posições. A verificação usa `-MaxLoss` internamente.
- `RsiPeriod` = 7 – Comprimento de média do indicador RSI.
- `RsiPrice` = High – Fonte de preço usada para o cálculo do RSI.
- `OverboughtLevel` = 88 – Nível de RSI que deve ser excedido antes de tomar uma reversão comprada.
- `OversoldLevel` = 37 – Nível de RSI que deve ser superado para baixo antes de tomar uma reversão vendida.
- `CandleType` = candles de 1 hora por padrão; ajustar para corresponder ao período do gráfico original.
