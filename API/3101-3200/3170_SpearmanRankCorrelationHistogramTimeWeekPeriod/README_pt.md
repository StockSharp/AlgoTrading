# Estratégia de Spearman Rank Correlation Histogram Time Window
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o especialista do MetaTrader **Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod** na API de alto nível do StockSharp. Ela subscreve um único fluxo de velas (padrão: barras de 4 horas) e avalia o histograma de correlação de posto de Spearman publicado no indicador MQL original. A cor do histograma determina se a tendência de curto prazo é altista (valores acima de zero) ou baixista (valores abaixo de zero). Uma janela de negociação dedicada mantém a atividade entre um intervalo configurável de dia da semana/hora, espelhando os controles `TimeTrade` do código fonte.

## Lógica de negociação
1. **Cálculo do indicador**
   - Em cada vela concluída a estratégia armazena o preço de fechamento e calcula a correlação de posto de Spearman sobre `RangeLength` fechamentos.
   - A cor do histograma é atribuída exatamente como no indicador: `4` quando a correlação está acima de `HighLevel`, `3` quando está entre `0` e `HighLevel`, `1` quando está entre `LowLevel` e `0`, `0` quando está abaixo de `LowLevel`, e `2` quando é exatamente zero.
   - Os sinais são avaliados na barra fechada número `SignalBar` (padrão: a barra que acabou de fechar). A barra fechada anterior é usada para detectar transições de cor.

2. **Modos de operação** – o parâmetro `TradeMode` controla como as cores são interpretadas:
   - **Mode1** – abrir comprados quando a cor salta acima de `2` depois de estar abaixo de `3`; abrir vendidos quando a cor cai abaixo de `2` depois de estar acima de `1`. Cada cor altista também solicita fechamento vendido, cada cor baixista fechamento comprado.
   - **Mode2** – abrir comprados na cor `4` (transição de qualquer coisa abaixo de `4`), abrir vendidos na cor `0` (transição de qualquer coisa acima de `0`). Cores maiores que `2` fecham vendidos; cores menores que `2` fecham comprados.
   - **Mode3** – abrir comprados na cor `4` e fechar vendidos ao mesmo tempo; abrir vendidos na cor `0` e fechar comprados simultaneamente.
   - Após uma entrada bem-sucedida a estratégia impõe um tempo de espera igual ao comprimento da vela (a próxima ordem na mesma direção é adiada até que a próxima barra tenha fechado no MetaTrader).

3. **Gestão de dinheiro e tamanho de ordem**
   - `MoneyManagement` combinado com `MarginMode` converte frações de patrimônio ou risco em um volume de ordem. Valores positivos seguem as regras de gestão de dinheiro originais, zero volta ao `Volume` da estratégia, e números negativos são interpretados como um tamanho de lote fixo.
   - Modos baseados em risco (`LossFreeMargin`, `LossBalance`) requerem um `StopLossPoints` positivo. Se o stop for zero a estratégia volta para `Volume` assim como o EA recusaria a operação.

4. **Gestão de risco**
   - `StopLossPoints` e `TakeProfitPoints` são traduzidos em níveis de preço usando `Security.PriceStep`. As saídas são verificadas em cada vela concluída usando a máxima/mínima da vela e todas as posições abertas são revertidas ao plano quando um nível é tocado.
   - `DeviationPoints` é preservado para completude da interface; ordens de mercado do StockSharp ignoram o valor.

5. **Janela de negociação semanal**
   - Quando `TimeTrade` é `true` o horário atual deve estar entre (`StartDay`, `StartHour`, `StartMinute`, `StartSecond`) e (`EndDay`, `EndHour`, `EndMinute`, `EndSecond`). Fora dessa janela todas as posições no instrumento da estratégia são fechadas imediatamente, correspondendo à saída de emergência original.
   - A implementação assume que `StartDay` não é posterior a `EndDay`. Para sessões sobrepostas (por exemplo Sexta → Segunda) ajustar os parâmetros adequadamente.

6. **Comportamento diverso**
   - Pelo menos `RangeLength + SignalBar + 1` velas concluídas devem estar disponíveis antes que os sinais possam ser gerados.
   - `Direction` é um interruptor reservado do indicador MQL; é mantido para paridade de parâmetros mas não tem efeito neste port.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `MoneyManagement` | Fração de capital ou tamanho de lote fixo para dimensionamento de posição. | `0.1` |
| `MarginMode` | Interpretação de `MoneyManagement` (`FreeMargin`, `Balance`, `LossFreeMargin`, `LossBalance`, `Lot`). | `Lot` |
| `StopLossPoints` | Distância de stop-loss em pontos de preço. | `1000` |
| `TakeProfitPoints` | Distância de take-profit em pontos de preço. | `2000` |
| `DeviationPoints` | Tolerância de slippage informativa em pontos. | `10` |
| `BuyOpen` / `SellOpen` | Habilitar abertura de posições compradas ou vendidas. | `true` |
| `BuyClose` / `SellClose` | Permitir fechamento de posições compradas ou vendidas em sinais. | `true` |
| `TradeMode` | Modo de interpretação do histograma (`Mode1`, `Mode2`, `Mode3`). | `Mode1` |
| `TimeTrade` | Ativar a janela de negociação semanal. | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | Início da janela (dia da semana e hora). | `Terça-feira`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | Fim da janela (dia da semana e hora). | `Sexta-feira`, `20`, `59`, `40` |
| `CandleType` | Período das velas processadas. | `H4` |
| `RangeLength` | Número de fechamentos usados pela correlação de Spearman. | `14` |
| `MaxRange` | `RangeLength` máximo permitido (guarda de segurança). | `30` |
| `Direction` | Sinalizador de indicador reservado, sem efeito no port. | `true` |
| `HighLevel`, `LowLevel` | Limiares superiores e inferiores do histograma. | `0.5`, `-0.5` |
| `SignalBar` | Número de barras fechadas para trás ao ler o buffer de cor. | `1` |

Toda a outra configuração da estratégia (seleção de portfólio, atribuição de segurança, `Volume` base, regras de risco) segue o fluxo de trabalho padrão do StockSharp.
