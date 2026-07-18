# Altarius RSI Stochastic Estratégia Dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Altarius RSI Stochastic Dual Strategy é uma conversão do MetaTrader consultor especialista `AltariusRSIxampnSTOH`. A lógica combina dois osciladores estocásticos com um filtro RSI de curto período. O estocástico lento identifica a direção da tendência e as zonas de sobrecompra/sobrevenda, enquanto o estocástico rápido mede a força do impulso. As saídas dependem de RSI e da linha de sinal estocástica lenta para rastrear negociações vencedoras e reduzir perdas. Recursos adicionais de gerenciamento de dinheiro refletem a lógica original MQL, reduzindo o tamanho da posição após perdas e aplicando um limite de levantamento de capital.

## Lógica de negociação

1. **Fonte de dados** – A estratégia funciona em velas configuráveis (barras padrão de 15 minutos). Todos os cálculos usam dados de fechamento de velas.
2. **Condições de entrada**
   - **Configuração longa**: A linha principal do estocástico lento (15,8,8) está acima de sua linha de sinal, mas ainda abaixo de `BuyStochasticLimit` (50 por padrão). O estocástico rápido (10,3,3) mostra o impulso com uma diferença absoluta entre as linhas principal e de sinal acima de `StochasticDifferenceThreshold` (5 por padrão).
   - **Configuração curta**: A linha principal do Slow Stochastic está abaixo de sua linha de sinal, mas permanece acima de `SellStochasticLimit` (55 por padrão). O estocástico rápido deve novamente mostrar uma diferença maior que o limite do momento.
3. **Exit Conditions**
   - **Saída longa**: acionada quando o RSI (período 4) excede `ExitRsiHigh` (60) e a linha de sinal estocástico lento cai abaixo de seu valor anterior enquanto permanece acima de `ExitStochasticHigh` (70).
   - **Saída curta**: Acionada quando RSI cai abaixo de `ExitRsiLow` (40) e a linha de sinal estocástica lenta sobe acima de seu valor anterior enquanto permanece abaixo de `ExitStochasticLow` (30).
   - **Saída de risco**: Se o PnL flutuante cair abaixo do rebaixamento de patrimônio permitido (`MaximumRiskPercent`), todas as posições serão achatadas imediatamente.
4. **Dimensionamento de posição** – Começa com `BaseVolume` e reduz o tamanho efetivo após perdas consecutivas em negociações via `DecreaseFactor`. As restrições de volume do corretor são respeitadas usando a etapa e os limites do volume de segurança.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `BaseVolume` | Tamanho base do pedido antes dos ajustes de gerenciamento de risco. |
| `MaximumRiskPercent` | Porcentagem do patrimônio da conta que pode ser perdida antes que a estratégia feche posições à força. |
| `DecreaseFactor` | Divisor que controla a rapidez com que o tamanho da posição se contrai após perdas consecutivas. |
| `RsiPeriod` | Comprimento RSI usado para decisões de saída. |
| `SlowStochasticPeriod`, `SlowStochasticK`, `SlowStochasticD` | Configuração do oscilador estocástico lento que orienta a direção da tendência. |
| `FastStochasticPeriod`, `FastStochasticK`, `FastStochasticD` | Configuração do oscilador estocástico rápido que mede o momento. |
| `StochasticDifferenceThreshold` | Distância mínima entre as linhas principal estocástica rápida e de sinal para confirmar o impulso. |
| `BuyStochasticLimit`, `SellStochasticLimit` | Níveis estocásticos lentos que definem a zona de negociação aceitável para novas posições. |
| `ExitRsiHigh`, `ExitRsiLow` | RSI níveis que preparam saídas longas ou curtas. |
| `ExitStochasticHigh`, `ExitStochasticLow` | Níveis de sinal estocásticos lentos que finalizam as saídas. |
| `CandleType` | Fonte de dados Candle para cálculos de indicadores. |

## Notas

- A estratégia negocia uma única posição de cada vez, espelhando o comportamento original do consultor especialista.
- Os ajustes de volume e a proteção contra rebaixamento são calculados usando as informações atuais do portfólio disponíveis em StockSharp.
- A visualização do gráfico desenha velas, osciladores estocásticos e marcadores comerciais quando uma área do gráfico está disponível.
