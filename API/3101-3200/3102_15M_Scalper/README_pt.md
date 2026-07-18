# Estratégia de Scalper de 15 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **15M Scalper** do MetaTrader para a API de alto nível do StockSharp. Recria a
lógica de entrada com múltiplos filtros (médias móveis ponderadas, oscilador estocástico, Parabolic SAR, momentum multi-temporal e
MACD mensal) e a pilha de saída completa que combina alvos baseados em dinheiro, stops de seguimento, movimentos de ponto de
equilíbrio e um guardião de drawdown de capital. A versão StockSharp opera em candles completadas exatamente como o EA e mantém o
código orientado a eventos preservando os parâmetros originais.

## Como Funciona

1. **Filtro de Tendência** – as médias móveis *ponderadas* rápida e lenta calculadas no período atual (padrão 15 minutos) devem
   estar alinhadas com a direção da operação. As médias usam o preço típico (`(High + Low + Close) / 3`) para corresponder à
   entrada `PRICE_TYPICAL` do MQL.
2. **Reversão Estocástica** – um oscilador estocástico 5/3/3 é amostrado nas duas últimas candles fechadas. Sinais comprados
   requerem que o %K cruze de volta acima de 20, enquanto os vendidos requerem um cruzamento abaixo de 80, espelhando as
   verificações `Stoc1`/`Stoc2` do script.
3. **Confirmação do Parabolic SAR** – o valor do SAR da barra completada deve estar abaixo da abertura anterior para comprados e
   acima para vendidos, reproduzindo o filtro de segurança `SAR < Open[1]` / `SAR > Open[1]`.
4. **Momentum em Período Superior** – um indicador de momentum de 14 períodos no período superior configurável (padrão 1 hora)
   deve se desviar de 100 em qualquer uma das últimas três barras fechadas pelo menos nos limiares de compra/venda. Isso
   implementa o trio `MomLevelB/MomLevelS` sem acessar os buffers de indicadores diretamente.
5. **MACD Mensal** – uma série de MACD no fluxo de candles mensais (padrão barras de 30 dias) mantém a linha principal acima do
   sinal para comprados e abaixo para vendidos. O mesmo filtro MACD também alimenta a lógica de saída opcional que fecha posições
   quando as linhas se cruzam na direção oposta.
6. **Gestão de Ordens** – quando uma configuração oposta aparece, a estratégia primeiro fecha a posição existente e depois aguarda
   a próxima barra para abrir operações na nova direção. O escalonamento de volume segue a regra de martingale do EA via
   `LotExponent` e o `IncreaseFactor` sensível a perdas.

## Gestão de Risco

- **Stop Loss / Take Profit** – as distâncias são inseridas em "pontos" do MetaTrader e são convertidas para preços absolutos
  através de `Security.PriceStep`. Para ticks fracionários de FX (passo de preço < 1), a implementação multiplica o passo por 10
  para imitar o tratamento de pips do EA.
- **Ponto de Equilíbrio ("sem perda")** – uma vez que o preço se move por `BreakEvenTriggerSteps`, o stop é movido virtualmente
  para a entrada mais o offset configurado. Se o preço recuar através desse nível, a posição é fechada a mercado.
- **Trailing Stop** – um trailing stop baseado em candles observa o máximo mais alto (para comprados) ou o mínimo mais baixo
  (para vendidos). Quando o recuo excede `TrailingStopSteps`, a posição é fechada, duplicando o comportamento original do
  `OrderModify`.
- **Alvos Monetários** – `UseProfitTargetMoney`, `UseProfitTargetPercent` e `EnableMoneyTrailing` trabalham com P&L flutuante
  medido via `PriceStep` × `StepPrice`. O port mantém intacta a lógica de take-profit, alvo percentual e drawdown de seguimento
  (`MoneyTrailingStop`).
- **Stop de Capital** – `UseEquityStop` rastreia o pico de (capital inicial + P&L realizado + lucro flutuante). Se o drawdown
  atual exceder `TotalEquityRisk` por cento desse pico, cada posição é fechada, replicando `AccountEquityHigh()` e
  `TotalEquityRisk` do EA.
- **Dimensionamento Martingale** – cada operação adicional na mesma direção escala o volume por `LotExponent`. Perdas
  consecutivas aumentam o próximo volume base em `IncreaseFactor` por perda, fornecendo o mesmo dimensionamento de lote
  "adaptativo" que o ramo `IncreaseFactor` do MQL.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período de trabalho principal (padrão candles de 15 minutos). |
| `MomentumCandleType` | Período superior para o filtro de momentum (padrão candles de 1 hora). |
| `MacdCandleType` | Período para o filtro de tendência MACD (padrão candles de 30 dias). |
| `FastMaPeriod`, `SlowMaPeriod` | Comprimentos das médias móviles ponderadas que definem o filtro de tendência. |
| `MomentumPeriod` | Comprimento do Momentum no período superior. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desvio absoluto mínimo de 100 necessário para permitir operações compradas/vendidas. |
| `StopLossSteps`, `TakeProfitSteps` | Distâncias de stop protetor e alvo em passos de preço. Definir como zero para desativar. |
| `TrailingStopSteps` | Distância do trailing stop em passos de preço. |
| `UseMoveToBreakeven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Indicador de ativação de ponto de equilíbrio, distância de ativação e offset. |
| `UseProfitTargetMoney`, `ProfitTargetMoney` | Ativar e configurar o alvo de lucro flutuante baseado em dinheiro. |
| `UseProfitTargetPercent`, `ProfitTargetPercent` | Ativar e configurar o alvo de lucro flutuante baseado em percentual. |
| `EnableMoneyTrailing`, `MoneyTrailingTakeProfit`, `MoneyTrailingStop` | Ativação do trailing monetário e máximo recuo permitido em moeda da conta. |
| `UseEquityStop`, `TotalEquityRisk` | Ativar o controle de drawdown de capital e definir o percentual permitido do capital máximo. |
| `BaseVolume`, `LotExponent`, `IncreaseFactor`, `MaxTrades` | Opções de dimensionamento martingale: lote inicial, multiplicador, incremento baseado em perdas e máximo de adições. |
| `UseExitByMacd` | Fechar posições quando a linha principal do MACD cruza o sinal contra a operação. |

## Uso

1. Conecte a estratégia a um ativo e certifique-se de que `Security.PriceStep` e `Security.StepPrice` estejam preenchidos. Esses
   valores são usados para traduzir entradas baseadas em pips e alvos monetários em números absolutos.
2. Ajuste `CandleType`, `MomentumCandleType` e `MacdCandleType` se quiser executar o scalper em diferentes períodos. Os padrões
   replicam a configuração original de 15 minutos / 1 hora / mensal.
3. Ajuste as distâncias baseadas em pips (`StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps`, configurações de ponto de
   equilíbrio) para se adequar ao tamanho de tick do instrumento. Comece com os padrões fornecidos e aumente-os para mercados
   mais voláteis.
4. Defina preferências de gestão de dinheiro: decida se usa take profits monetários ou percentuais, ative o trailing monetário e
   configure o stop de capital se quiser uma rede de segurança contra drawdowns profundos.
5. Lance a estratégia. Ela se inscreverá automaticamente em todos os fluxos de candles necessários, plotará indicadores (se um
   gráfico estiver disponível) e começará a avaliar sinais assim que cada indicador tiver histórico suficiente.

## Notas e Diferenças do EA Original

- O port usa o modelo de posição agregada do StockSharp. Quando um sinal oposto aparece, a posição atual é fechada primeiro e a
  nova direção é avaliada na próxima candle, mantendo o comportamento determinístico.
- Cálculos baseados em dinheiro dependem de `Security.PriceStep` e `Security.StepPrice`. Se o local não fornecer esses valores,
  os alvos monetários são ignorados (o lucro flutuante é relatado como zero), exatamente como indicado nos comentários do código.
- `IncreaseFactor` adiciona `IncreaseFactor × perdas_consecutivas` ao próximo volume base em vez de usar margem livre (que não
  está disponível no ambiente sandbox). Isso ainda captura a intenção de aumentar o tamanho após sequências de perdas.
- Todas as decisões são tomadas em candles finalizadas para evitar dupla contagem de sinais, correspondendo às verificações barra
  por barra da implementação MetaTrader.
- A estratégia desenha os mesmos indicadores no gráfico quando um visualizador está disponível, auxiliando na depuração e
  tornando o port fácil de comparar com o EA.

Revise cuidadosamente o tamanho de tick, o preço do passo e as restrições de volume do seu corretor antes do trading ao vivo.
Esses valores impactam diretamente como as distâncias baseadas em pips e os alvos monetários são convertidos dentro da estratégia.
