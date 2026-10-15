# Estratégia EMA WMA Contrarian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema contrário de cruzamento que compara uma média móvel exponencial (EMA) e uma média móvel ponderada (WMA) construídas sobre preços de abertura de velas. Quando a EMA rápida cai abaixo da WMA, a estratégia compra apostando em um retorno. Quando a EMA sobe de volta acima da WMA, entra vendido. O tamanho da operação é derivado do percentual de risco configurado e da distância ao stop protetor, enquanto os níveis opcionais de stop-loss, take-profit e trailing stop mantêm a exposição sob controle.

## Detalhes

- **Critérios de entrada**:
  - Comprado: EMA(Abertura) cruza de cima para baixo da WMA(Abertura)
  - Vendido: EMA(Abertura) cruza de baixo para cima da WMA(Abertura)
- **Comprado/Vendido**: Ambas as direções
- **Critérios de saída**:
  - Stop-loss fixo em passos de preço
  - Take-profit fixo em passos de preço
  - Trailing stop que avança após o preço se mover `TrailingStopPoints + TrailingStepPoints`
  - O cruzamento oposto fecha a posição atual e abre a nova
- **Stops**: Stop-loss, take-profit e trailing stop
- **Valores padrão**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Média Móvel, Contrarian
  - Direção: Comprado e Vendido
  - Indicadores: EMA (abertura), WMA (abertura)
  - Stops: Sim (stop fixo, trailing)
  - Complexidade: Intermediário
  - Período: Intradiário (padrão 1 minuto)
  - Sazonalidade: Nenhum
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | Períodos de retrospectiva para a EMA e WMA calculadas nas aberturas de velas. |
| `StopLossPoints`, `TakeProfitPoints` | Distância em passos de preço para colocar o stop-loss protetor e a meta de lucro. |
| `TrailingStopPoints` | Distância entre o preço e o trailing stop após a ativação. |
| `TrailingStepPoints` | Movimento favorável adicional necessário antes que o trailing stop suba/desça. Deve ser positivo quando o trailing está habilitado. |
| `RiskPercent` | Percentual do capital do portfólio arriscado por operação. O tamanho da posição é calculado como `RiskPercent / (StopLossPoints * PriceStep)`. |
| `BaseVolume` | Tamanho mínimo de operação usado quando o dimensionamento baseado em risco não pode ser determinado. |
| `CandleType` | Tipo de dados de vela para cálculos (padrão 1 minuto). |

## Notas

- Ambas as médias móveis consomem preços de abertura de velas, espelhando o assessor especialista original do MetaTrader.
- Os trailing stops só são ativados após o preço se mover pelo menos `TrailingStopPoints + TrailingStepPoints` a favor da operação, replicando a lógica legada.
- Se `TrailingStopPoints` estiver definido enquanto `TrailingStepPoints` é zero ou negativo, a estratégia para imediatamente para evitar comportamento de trailing inconsistente.
- O dimensionamento baseado em risco recorre ao `BaseVolume` se o valor do portfólio, o passo de preço ou a distância do stop não estiverem disponíveis.
