# Estratégia Altarius RSI Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Altarius RSI Stochastic é uma conversão direta do consultor especializado MetaTrader 5 "Altarius RSI Stohastic" para a API de alto nível do StockSharp. O sistema sincroniza dois osciladores Stochastic com um RSI rápido de 3 períodos para capturar reversões de curta duração que ocorrem quando o momentum se comprime e depois se expande novamente. A implementação no StockSharp preserva a lógica original de entrada e saída, adicionando conveniências modernas como parâmetros de estratégia, gerenciamento automático de risco e dimensionamento adaptativo de posição.

## Como funciona
- **Stochastic primário (15/8/8):** Atua como filtro de tendência. Posições compradas exigem que a linha %K esteja abaixo de 50 e cruze acima da linha %D, sinalizando momentum ascendente em uma zona neutra a sobrevendida. Posições vendidas exigem a condição espelhada acima de 55.
- **Stochastic secundário (10/3/3):** Mede o quanto %K diverge de %D. Um diferencial absoluto mínimo de 5 pontos é necessário para validar o momentum antes de entrar em uma posição.
- **RSI (Período 3):** Controla as saídas. Posições compradas são fechadas quando o RSI supera 60 e o %D primário vira para baixo a partir de acima de 70. Posições vendidas saem quando o RSI cai abaixo de 40 e o %D primário vira para cima a partir de abaixo de 30.
- **Guarda de Drawdown:** Se o PnL flutuante cair abaixo do múltiplo de risco configurável do patrimônio da conta, a estratégia liquida imediatamente a posição aberta, similar ao stop de emergência do código original.
- **Dimensionamento adaptativo:** O volume inicial é derivado do patrimônio do portfólio multiplicado pelo fator `MaximumRisk` e dividido por 1000, seguindo a abordagem do MT5. Operações perdedoras consecutivas reduzem o tamanho da posição de acordo com o `DecreaseFactor`, respeitando um volume mínimo negociável.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período utilizado para assinaturas de velas. | Período de 5 minutos |
| `BaseVolume` | Volume de reserva usado quando informações do portfólio não estão disponíveis. | 0.1 |
| `MinimumVolume` | Volume mínimo permitido após todos os cálculos. | 0.1 |
| `MaximumRisk` | Multiplicador de risco aplicado ao valor do portfólio para dimensionamento e saída por drawdown. | 0.1 |
| `DecreaseFactor` | Divisor que reduz o volume após operações perdedoras consecutivas. | 3 |
| `PrimaryStochasticLength` | Período de lookback para a linha %K do Stochastic primário. | 15 |
| `PrimaryStochasticKPeriod` | Suavização para a linha %K primária. | 8 |
| `PrimaryStochasticDPeriod` | Período para a linha de sinal %D primária. | 8 |
| `SecondaryStochasticLength` | Período de lookback para o Stochastic de confirmação. | 10 |
| `SecondaryStochasticKPeriod` | Suavização para a linha %K secundária. | 3 |
| `SecondaryStochasticDPeriod` | Período para a linha %D secundária. | 3 |
| `DifferenceThreshold` | Diferencial mínimo entre %K e %D secundários para permitir entradas. | 5 |
| `PrimaryBuyLimit` | Valor máximo de %K primário permitido antes de abrir uma posição comprada. | 50 |
| `PrimarySellLimit` | Valor mínimo de %K primário permitido antes de abrir uma posição vendida. | 55 |
| `PrimaryExitUpper` | Limiar de %D primário que deve ser excedido antes de fechar compradas. | 70 |
| `PrimaryExitLower` | Limiar de %D primário que deve ser ficado abaixo antes de fechar vendidas. | 30 |
| `RsiPeriod` | Comprimento de lookback do RSI. | 3 |
| `LongExitRsi` | Nível de RSI que confirma saídas de compradas. | 60 |
| `ShortExitRsi` | Nível de RSI que confirma saídas de vendidas. | 40 |

## Regras de trading
1. **Critérios de entrada**
   - **Comprado:** %K primário > %D primário, %K primário < `PrimaryBuyLimit`, e |%K secundário − %D secundário| > `DifferenceThreshold` enquanto a estratégia está flat.
   - **Vendido:** %K primário < %D primário, %K primário > `PrimarySellLimit`, e |%K secundário − %D secundário| > `DifferenceThreshold` enquanto a estratégia está flat.
2. **Critérios de saída**
   - **Saída comprado:** RSI > `LongExitRsi`, %D primário > `PrimaryExitUpper`, e o valor atual de %D é inferior ao da vela anterior.
   - **Saída vendido:** RSI < `ShortExitRsi`, %D primário < `PrimaryExitLower`, e o valor atual de %D é superior ao da vela anterior.
   - **Saída por risco:** Quando a perda flutuante excede `MaximumRisk × Portfolio.CurrentValue`.

## Gerenciamento de risco
- A estratégia chama automaticamente `StartProtection()` para ativar os serviços de proteção de posição integrados do StockSharp.
- O tamanho da posição diminui quando `_lossStreak` excede uma operação perdedora consecutiva, imitando a lógica `DecreaseFactor` do MT5.
- `MinimumVolume` impede que o tamanho da posição caia abaixo dos requisitos de tamanho mínimo de tick da bolsa.

## Notas
- A estratégia assume um portfólio com capacidade de hedge, exatamente como o EA original.
- Personalize o parâmetro `CandleType` para corresponder ao período que você teria usado no MetaTrader (M1, M5, etc.).
- Combine este módulo com o StockSharp Designer ou o projeto Backtester neste repositório para validar o desempenho com seus próprios dados.
