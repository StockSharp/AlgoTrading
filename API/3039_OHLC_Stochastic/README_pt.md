# Estratégia OHLC Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de seguimento de momentum que usa o oscilador Stochastic clássico %K/%D em candles OHLC.
O algoritmo reage a cruzamentos em zonas de sobrecompra/sobrevenda e protege as operações abertas com um trailing stop configurável medido em passos de preço.

## Detalhes

- **Ideia principal**: explorar a mudança de momentum quando o Stochastic %K cruza %D em níveis extremos.
- **Critérios de entrada**:
  - **Comprado**:
    - %K cruza acima de %D e pelo menos uma das linhas está abaixo do limiar `LevelDown`.
    - Se existir uma posição vendida, ela é fechada e revertida para comprada.
  - **Vendido**:
    - %K cruza abaixo de %D e pelo menos uma das linhas está acima do limiar `LevelUp`.
    - Se existir uma posição comprada, ela é fechada e revertida para vendida.
- **Critérios de saída**:
  - O trailing stop é acionado (com base na distância `TrailingStopSteps` e no requisito de melhoria `TrailingStepSteps`).
  - O sinal de entrada oposto aparece, desencadeando uma reversão.
- **Lógica de trailing**:
  - A distância e o passo são multiplicados pelo `PriceStep` do instrumento para converter pips/passos em preços absolutos.
  - O stop só avança depois que a operação se move além de `TrailingStopSteps + TrailingStepSteps` do preço de entrada.
  - Lógica de trailing separada para os lados comprado e vendido.
- **Indicadores**:
  - [StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm) com `KPeriod`, `DPeriod` e `Slowing` ajustáveis.
- **Comprado/Vendido**: Ambos.
- **Stops**: Apenas trailing stop (sem ordens fixas de SL/TP).
- **Dimensionamento de posição**: Usa o parâmetro `Volume` da estratégia; as reversões enviam `Volume + |Position|` para mudar de direção.
- **Parâmetros padrão**:
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5 (passos de preço)
  - `TrailingStepSteps` = 2 (passos de preço)
- **Visualização**:
  - Desenha candles OHLC, indicador Stochastic e marcadores de operações quando os gráficos estão disponíveis.

## Notas de uso

1. Configure o instrumento subjacente e o período antes de iniciar a estratégia.
2. Ajuste `TrailingStopSteps` de acordo com o tamanho do tick do instrumento para refletir distâncias reais em pips.
3. A estratégia chama `StartProtection()` para que regras de risco adicionais possam ser anexadas externamente.
4. Funciona melhor em regimes de tendência onde as reversões do Stochastic lideram o preço.
5. Para produtos intradiários, períodos mais baixos podem exigir a redução das distâncias de trailing para evitar saídas prematuras.
