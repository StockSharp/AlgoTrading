# Estratégia Dealers Trade MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Dealers Trade MACD é um sistema de piramidação que foi portado do consultor especializado MQL5 original "Dealers Trade v7.74". Ele segue a inclinação da linha principal MACD para decidir quando acumular posições na direção da tendência. A lógica é projetada para swing trading em gráficos H4 e D1 onde as mudanças de momentum são menos ruidosas.

## Como a estratégia funciona

- **Geração de sinais** – a estratégia subscreve velas do período selecionado e avalia o valor da linha principal MACD em cada barra fechada. Um MACD ascendente implica viés comprado e um MACD descendente implica viés vendido. O sinal pode ser invertido com o parâmetro `ReverseCondition` para corresponder a contas que historicamente operaram entradas contrárias.
- **Dimensionamento de posição** – a primeira ordem usa o tamanho fixo `FixedVolume` ou, se estiver em `0`, o sistema aloca risco dinamicamente do capital do portfólio usando o parâmetro `RiskPercent` e a distância de stop loss configurada. Entradas adicionais são multiplicadas por `VolumeMultiplier` elevado à contagem de posições atuais (p.ex. 1.6, 1.6², 1.6³, …) e são enviadas apenas quando o preço se moveu pelo menos `IntervalPoints * PriceStep` desde o último preenchimento. As ordens são ignoradas quando a exposição líquida ultrapassaria `MaxVolume` ou o número de entradas atinge `MaxPositions`.
- **Gerenciamento de ordens** – cada posição mantém seus próprios alvos de stop loss e take profit calculados a partir do preço de entrada e dos offsets baseados em pontos (`StopLossPoints`, `TakeProfitPoints`). Se `TrailingStopPoints` for maior que zero, o stop é puxado para cima (ou para baixo para vendidos) uma vez que o lucro ultrapasse `TrailingStopPoints + TrailingStepPoints`, emulando o comportamento de trailing original.
- **Proteção de conta** – quando o número de trades abertos é maior que `PositionsForProtection` e o lucro não realizado agregado cruza `SecureProfit`, a estratégia fecha a posição mais lucrativa para fixar ganhos antes de adicionar nova exposição. Isso reflete o bloco de "Proteção de conta" da versão MQL.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | H4 | Período usado para cálculos MACD e decisões de negociação. |
| `FixedVolume` | 0.1 | Tamanho de lote para a primeira entrada. Definir como 0 para habilitar dimensionamento baseado em risco. |
| `RiskPercent` | 5 | Porcentagem do capital atual arriscado quando `FixedVolume` é zero. |
| `StopLossPoints` | 90 | Distância do stop loss expressa em passos de preço. Usar 0 para desabilitar stops fixos. |
| `TakeProfitPoints` | 30 | Distância do take profit em passos de preço. Usar 0 para desabilitar. |
| `TrailingStopPoints` | 15 | Distância do trailing stop em passos de preço. Definir como 0 para desativar o trailing. |
| `TrailingStepPoints` | 5 | Distância adicional que deve ser ganha antes do trailing stop se mover novamente. |
| `MaxPositions` | 5 | Número máximo de entradas abertas simultaneamente. |
| `IntervalPoints` | 15 | Distância mínima em passos de preço necessária entre entradas consecutivas. |
| `SecureProfit` | 50 | Limiar de lucro (em moeda de cotação) que aciona a proteção de conta. |
| `AccountProtection` | true | Habilita fechar a negociação com melhor desempenho quando o alvo de lucro seguro é atingido. |
| `PositionsForProtection` | 3 | Número mínimo de trades que devem estar abertos antes que a proteção possa ser acionada. |
| `ReverseCondition` | false | Inverte a interpretação da inclinação MACD. |
| `MacdFastPeriod` | 14 | Comprimento EMA rápida para o indicador MACD. |
| `MacdSlowPeriod` | 26 | Comprimento EMA lenta para o indicador MACD. |
| `MacdSignalPeriod` | 1 | Comprimento EMA de sinal para o indicador MACD (definido como 1 no consultor especializado original). |
| `MaxVolume` | 5 | Limite superior para o tamanho de posição acumulado. |
| `VolumeMultiplier` | 1.6 | Multiplicador aplicado ao tamanho base para cada nova entrada. |

## Notas e limitações

- O especialista MQL original era capaz de manter posições longas e vendidas hedgeadas simultaneamente. O StockSharp usa posições líquidas por padrão, portanto este port fecha a exposição oposta antes de adicionar novos trades na outra direção.
- Os valores MACD são avaliados apenas em velas fechadas. Sinais intrabarra podem aparecer mais tarde do que na implementação MQL baseada em ticks, mas o comportamento é muito mais estável para testes históricos.
- Todas as distâncias baseadas em pontos são multiplicadas pelo `PriceStep` do instrumento. Se o instrumento não fornecer esses metadados, a estratégia recorre a um passo de 0.0001, então ajuste os parâmetros ao negociar instrumentos com diferentes tamanhos de tick.
- Quando `FixedVolume` é zero, a estratégia requer uma distância de stop loss não-zero para calcular o dimensionamento baseado em risco. Se o stop estiver desabilitado, o volume padrão é zero e nenhum trade é enviado.
