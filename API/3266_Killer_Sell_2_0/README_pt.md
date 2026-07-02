# Killer Sell 2.0 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Killer Sell 2.0 é um consultor especialista MetaTrader 4 somente-vendido que cronometra entradas após leituras estendidas de sobrecompra e assegura lucros quando o momentum oscila para território de sobrevenda. Este port reescreve a lógica original sobre a API de estratégia de alto nível do StockSharp. Todo o processamento de indicadores é orientado por eventos através de `SubscribeCandles().BindEx(...)`, e as regras de gerenciamento de dinheiro estão encapsuladas dentro da classe de estratégia.

## Lógica de trading
A lógica convertida segue a cadeia de sinais original enquanto usa o modelo de posição líquida do StockSharp. Cada vela completada do período configurado executa os seguintes passos:

1. **Preparação de dados.** A estratégia atualiza um MACD (12/120/9), Williams %R (período 350 para ambos os filtros) e dois osciladores Estocásticos (10/1/3 para entrada, 90/7/1 para saídas). Os valores dos indicadores são consumidos apenas quando a nova barra está concluída e as entradas estão completamente formadas.
2. **Filtro de entrada.** Um setup vendido é válido quando todas as condições abaixo são atendidas:
   - Williams %R sobe acima de −10, sinalizando um mercado sobrecomprado.
   - A linha principal do MACD é maior que `0.0014`.
   - O %K do Estocástico de entrada cruza **abaixo** do nível de entrada configurável (padrão 90). A detecção de cruzamento é realizada em leituras consecutivas de %K.
3. **Colocação de ordem.** Quando os filtros se alinham, a estratégia envia uma venda de mercado usando o tamanho de lote martingale atual. As ordens herdam um take-profit definido `N` pips adiante (padrão 100 pips) via `StartProtection`.
4. **Gerenciamento de saída.** Enquanto existe exposição vendida, a estratégia calcula a média aritmética do lucro em pips dos tickets abertos. Dependendo do momentum:
   - Se o lucro médio estiver **abaixo** de 10 pips e Williams %R cair abaixo de −80, todas as vendidas são fechadas imediatamente.
   - Se o lucro médio estiver **acima** de 15 pips e o %K do Estocástico de saída cair abaixo de 12, a posição é fechada para assegurar o ganho.

## Gerenciamento de dinheiro
Killer Sell 2.0 usa uma escada martingale semelhante ao EA original. A implementação StockSharp mantém uma lista interna de lotes vendidos abertos para imitar os cálculos por ticket do MetaTrader:

- A primeira operação usa `InitialVolume` (padrão 0.05 lotes).
- Após um ciclo lucrativo ou break-even, o volume é redefinido para o tamanho de lote inicial.
- Após um ciclo perdedor, a próxima ordem é multiplicada por `MartingaleMultiplier` (padrão ×1.2). Um limite de segurança `MaxVolume` previne crescimento descontrolado.

O auxiliar também rastreia PnL realizado em preenchimentos para decidir se o ciclo anterior foi lucrativo.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Período principal que alimenta cada indicador. |
| `EntryWprPeriod` / `ExitWprPeriod` | Comprimentos de Williams %R para confirmações de entrada e saída. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuração do MACD. |
| `MacdThreshold` | Valor mínimo da linha principal do MACD necessário para uma venda. |
| `StochasticEntryKPeriod`, `StochasticEntryDPeriod`, `StochasticEntrySlow` | Parâmetros do Estocástico de entrada. |
| `EntryStochasticLevel` | Nível que %K deve cruzar de cima para validar um sinal. |
| `StochasticExitKPeriod`, `StochasticExitDPeriod`, `StochasticExitSlow` | Parâmetros do Estocástico de saída. |
| `ExitStochasticLevel` | Limite de sobrevenda verificado antes de assegurar lucros. |
| `EntryWprThreshold` / `ExitWprThreshold` | Limiares de Williams %R para entradas/saídas. |
| `LossExitPips` / `ProfitExitPips` | Limites de lucro médio (em pips) controlando saídas defensivas e de alvo. |
| `TakeProfitPips` | Take-profit protetor atribuído a cada ordem de venda. |
| `InitialVolume` | Volume do primeiro passo martingale. |
| `MartingaleMultiplier` | Fator aplicado após perdas. |
| `MaxVolume` | Limite absoluto aplicado ao próximo tamanho de lote. |

## Notas de conversão
- MetaTrader mantém tickets individuais; StockSharp trabalha com uma posição líquida. A estratégia portanto armazena cada vendido preenchido (volume + preço) para reproduzir cálculos de lucro médio e para avaliar resets martingale.
- O bloco "martingale" do MT4 expunha muitos modos adicionais (fixo, risco percentual, 1326, Fibonacci, etc.). A configuração original usava o ramo martingale simples; apenas esse comportamento é replicado aqui.
- O stop-loss de emergência estava desabilitado no projeto fonte. O port espelha essa configuração anexando apenas um take-profit e gerenciando outras saídas internamente.

## Dicas de uso
1. Anexe a estratégia a um portfólio e ativo, depois configure o mesmo período usado nos backtests do MT4 (os padrões assumem H1).
2. Certifique-se de que os dados de mercado entregam velas completadas; os indicadores dependem de eventos `CandleStates.Finished`.
3. Revise a alavancagem da conta e os tamanhos de lote permitidos. O limite martingale padrão (5 lotes) deve ser ajustado aos requisitos do seu corretor.
4. Faça backtests extensivamente — estratégias martingale amplificam o risco quando os mercados têm tendência forte contra o viés vendido.
