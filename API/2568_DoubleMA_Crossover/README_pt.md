# Estratégia de Rompimento por Cruzamento Duplo de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz o consultor especialista MetaTrader "DoubleMA Crossover" dentro do framework do StockSharp. A lógica monitora uma média móvel rápida e uma lenta, aguarda um cruzamento direcional e então requer uma confirmação de rompimento antes de entrar no mercado. O algoritmo gerencia apenas uma posição de cada vez e inclui comportamento opcional de trailing stop que imita os três modos de trailing originais.

## Como funciona

1. **Detecção de sinal** – Duas médias móveis simples (padrões: 2 e 5) são calculadas na série de velas selecionada. Um cruzamento altista ocorre quando a média rápida cruza acima da lenta e vice-versa para um cruzamento baixista.
2. **Confirmação de rompimento** – Após um cruzamento, a estratégia armazena um nível de rompimento definido em passos de preço (`BreakoutPips`). Uma posição é aberta somente quando o preço alcança esse nível em uma vela subsequente, replicando o comportamento da ordem stop da versão MQL.
3. **Gestão de posição** – Apenas uma única posição é permitida. Enquanto uma operação está ativa, a estratégia monitora stop loss, take profit e o tipo de trailing stop configurado. Os rastreadores internos emulam a execução do lado do corretor para manter o comportamento determinístico nos backtests.
4. **Filtro de sessão** – A negociação pode ser restrita a uma janela de tempo específica (`StartHour`..`StopHour`). A estratégia ainda gerencia operações abertas fora da janela, mas não cria novos níveis de rompimento quando o filtro bloqueia a negociação.
5. **Trailing stops** – Três modos de trailing são suportados: trailing imediato com a distância de stop inicial, trailing após uma distância personalizada e a lógica de três níveis com mudanças de break-even igual ao EA original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Períodos das médias móveis simples rápida e lenta. |
| `BreakoutPips` | Distância em passos de preço adicionada ao fechamento da vela de sinal para definir o gatilho de rompimento. |
| `StopLossPips`, `TakeProfitPips` | Stop protetor e take profit opcional em passos de preço. Definir take profit como zero para desabilitá-lo. |
| `UseTrailingStop` | Habilita o gerenciamento do trailing stop. |
| `TrailingMode` | Tipo de trailing: Type1 usa a distância de stop original, Type2 aguarda uma distância personalizada (`TrailingStopPips`), Type3 usa os três níveis MQL. |
| `TrailingStopPips` | Distância para o trailing de Type2. |
| `Level1TriggerPips`, `Level1OffsetPips` | Primeiro nível de gatilho e offset para o trailing de Type3 (move o stop para break-even por padrão). |
| `Level2TriggerPips`, `Level2OffsetPips` | Segundo nível de gatilho e offset para o trailing de Type3. |
| `Level3TriggerPips`, `Level3OffsetPips` | Terceiro nível de gatilho e offset para o trailing de Type3 (converte para um trailing stop clássico). |
| `UseTimeLimit`, `StartHour`, `StopHour` | Habilita o filtro de sessão de negociação e define o intervalo de horas inclusivo. |
| `CandleType` | Série de velas usada para cálculos de sinal. |
| `TradeVolume` | Volume de ordem expresso em lotes. |

## Modos de Trailing Stop

- **Type1** – Move o stop usando a distância original de stop loss uma vez que o preço avança essa quantidade.
- **Type2** – Aguarda até que o preço se mova `TrailingStopPips` antes de traillar, depois bloqueia o lucro nessa distância.
- **Type3** – Usa três níveis: os dois primeiros deslocam o stop pelos offsets definidos, e o terceiro converte para um trailing stop contínuo usando o fechamento atual e `Level3OffsetPips`.

## Dicas de uso

- Alinhar `BreakoutPips` com o tamanho de tick do instrumento para manter o mesmo comportamento do consultor especialista MetaTrader.
- Revisar o filtro de sessão para corresponder aos horários de negociação; o padrão permite entradas entre 11:00 e 16:00 hora local.
- Desabilitar o filtro de tempo (`UseTimeLimit = false`) para instrumentos 24/7.
- Ao testar o trailing tipo 3, garantir que os valores de offset não sejam maiores que seus níveis de gatilho correspondentes; caso contrário, o stop pode permanecer atrás do preço de entrada.
