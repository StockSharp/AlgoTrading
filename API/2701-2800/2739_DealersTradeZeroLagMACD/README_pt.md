# Estratégia DealersTradeZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o expert advisor do MetaTrader "Dealers Trade v 7.91 ZeroLag MACD" para a API de alto nível do StockSharp. Ela rastreia a inclinação de um MACD de zero atraso para decidir se o mercado está em uma fase de acumulação para comprados ou vendidos e constrói uma grade de posições com espaçamento adaptativo e gestão de risco. O período padrão são velas de quatro horas conforme recomendado pelo autor original, mas qualquer tipo de vela suportado pelo StockSharp pode ser selecionado.

## Lógica de trading
- **Detecção de sinal.** Duas médias móveis exponenciais de zero atraso (rápida e lenta) geram uma linha MACD. Quando o MACD sobe em comparação com a barra anterior, a estratégia trata o mercado como de alta; quando cai, trata como de baixa. O sinal pode ser invertido via o parâmetro `ReverseCondition`.
- **Grade de posições.** O algoritmo escala na direção detectada. As distâncias entre entradas são medidas em pips e multiplicadas após cada preenchimento por `IntervalCoefficient`. O tamanho do lote é multiplicado por `LotMultiplier` em cada entrada adicional, imitando o esquema martingale da versão MQL.
- **Controle de volume.** Se `BaseVolume` for maior que zero, é usado como quantidade inicial da ordem. Caso contrário, o motor deriva o tamanho de `RiskPercent`, distância do stop e parâmetros de passo do instrumento. Cada volume calculado é verificado contra os limites do instrumento e limitado por `MaxVolume`.
- **Gerenciamento de ordens.** Cada entrada pode ser equipada com stop-loss, take-profit e trailing stop (todos em pips). A distância do take-profit é multiplicada por `TakeProfitCoefficient` para entradas sucessivas para ampliar os alvos.
- **Proteção de conta.** Quando o número total de posições abertas excede `PositionsForProtection` e o lucro combinado atinge `SecureProfit`, a estratégia fecha a operação com maior lucro para garantir ganhos. Se o número total de posições exceder `MaxPositions`, fecha a pior operação antes de aceitar novas entradas.

## Tratamento de posições
- Stops, lógica de trailing e alvos são avaliados em velas terminadas usando preços de fechamento, máximo e mínimo.
- Todas as posições abertas são rastreadas com seu próprio volume, preço de entrada e estado de trailing. O último preço de preenchimento é reutilizado para reforçar o espaçamento mínimo para entradas futuras.
- Quando o saldo da conta cai abaixo de `MinimumBalance`, a estratégia se para para evitar o sobretrading em contas pequenas.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `BaseVolume` | Tamanho inicial da ordem. Definir como zero para habilitar o dimensionamento baseado em risco via `RiskPercent`. |
| `RiskPercent` | Porcentagem do patrimônio do portfólio a arriscar quando o tamanho da posição é derivado da distância do stop. |
| `MaxPositions` | Número máximo de entradas abertas simultaneamente. |
| `IntervalPips` | Espaçamento inicial entre entradas da grade em pips. |
| `IntervalCoefficient` | Multiplicador aplicado ao espaçamento após cada entrada adicional. |
| `StopLossPips` | Distância do stop-loss em pips. Definir como zero para desabilitar. |
| `TakeProfitPips` | Distância base do take-profit em pips. Multiplicada por `TakeProfitCoefficient` por entrada. |
| `TrailingStopPips` / `TrailingStepPips` | Distância do trailing stop e avanço necessário antes do trailing ser ajustado. |
| `TakeProfitCoefficient` | Multiplicador para ampliar distâncias de take-profit em entradas posteriores. |
| `SecureProfit` | Limiar de lucro que ativa a proteção de conta quando há posições suficientes abertas. |
| `AccountProtection` | Habilita o aseguramento automático de lucros fechando a melhor operação. |
| `PositionsForProtection` | Número mínimo de posições abertas necessárias antes de a proteção de conta se ativar. |
| `ReverseCondition` | Inverte a interpretação da inclinação do MACD. |
| `FastLength`, `SlowLength`, `SignalLength` | Períodos das médias móveis exponenciais de zero atraso. |
| `MaxVolume` | Limite para o volume de uma única entrada. |
| `LotMultiplier` | Fator multiplicativo para escalar o tamanho da posição com cada entrada da grade. |
| `MinimumBalance` | Saldo mínimo de conta necessário para continuar operando. |
| `CandleType` | Tipo de dados de vela usado para os cálculos. |

## Notas de uso
1. Conecte a estratégia a um portfólio e instrumento antes de iniciá-la.
2. Revise o passo do instrumento e as configurações de preço para garantir que as conversões de pips estão corretas.
3. Os parâmetros padrão replicam o comportamento do expert advisor original, mas podem ser otimizados através dos otimizadores do StockSharp.
4. A tradução para Python não está incluída para esta estratégia.
