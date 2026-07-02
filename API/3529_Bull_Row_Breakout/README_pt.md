# Estratégia de ruptura da Bull Row
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Bull Row Breakout é uma conversão C# do consultor especialista MetaTrader 5 "BULL row full EA". O robô original foi construído com um construtor de blocos e combina padrões de ação de preço com confirmação de impulso. A porta StockSharp reproduz a mesma lógica em um único período configurável e mantém os comentários de negociação em inglês, conforme necessário.

A estratégia abre posições **somente longas** após uma sequência de velas de baixa ser seguida por um impulso de alta e um rompimento acima das máximas recentes. Os filtros do oscilador Stochastic controlam a força do impulso enquanto o stop loss dinâmico e os níveis alvo recriam as configurações de risco da versão MQL.

## Lógica de Sinais
1. Aguarde o fechamento de uma nova vela (execução "uma vez por barra").
2. Verifique se nenhuma posição longa está aberta no momento.
3. Detectar uma linha de baixa:
   - `BearRowSize` velas consecutivas começando em `BearShift` barras atrás devem ser de baixa.
   - Cada corpo de vela deve ter pelo menos `BearMinBody` etapas de preço.
   - A progressão corporal deve satisfazer o `BearRowMode` selecionado (normal/maior/menor).
4. Detectar uma linha de alta:
   - `BullRowSize` velas consecutivas começando em `BullShift` barras atrás devem ser de alta.
   - Cada corpo de vela deve ter pelo menos `BullMinBody` etapas de preço.
   - A progressão corporal deve satisfazer `BullRowMode`.
5. Confirmação de rompimento: o fechamento da última vela finalizada deve ser superior à máxima mais alta registrada da barra 2 até `BreakoutLookback` barras atrás.
6. Stochastic confirmação:
   - O %K atual (`StochasticKPeriod`) deve estar acima de %D (`StochasticDPeriod`).
   - Os últimos valores de `StochasticRangePeriod` %K devem ficar entre `StochasticLowerLevel` e `StochasticUpperLevel`.
7. Gestão de risco:
   - O preço Stop é o mínimo mais baixo entre as últimas `StopLossLookback` velas (começando na última barra fechada).
   - O Take Profit é colocado a uma distância igual a `TakeProfitPercent` por cento da distância de parada.
   - O stop e o alvo são monitorados em cada vela fechada; se qualquer um dos níveis intrabar for atingido, a posição será fechada no mercado na próxima atualização.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Volume de negociação fixo usado para cada entrada. |
| `CandleTimeFrame` | Prazo das velas processadas. |
| `StopLossLookback` | Número de barras usadas para calcular o preço de stop dinâmico. |
| `TakeProfitPercent` | Distância de recompensa expressa como uma porcentagem da distância de parada. |
| `BearRowSize`, `BearMinBody`, `BearRowMode`, `BearShift` | Configuração da linha de baixa que precede o rompimento. |
| `BullRowSize`, `BullMinBody`, `BullRowMode`, `BullShift` | Configuração da linha de alta que precede imediatamente o sinal. |
| `BreakoutLookback` | Comprimento da alta rolante usada para confirmação de rompimento. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Stochastic Configurações do oscilador. |
| `StochasticRangePeriod` | Número de valores históricos Stochastic que devem permanecer dentro dos limites. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Limites do canal do oscilador aplicados a %K. |

Todos os tamanhos de corpo são expressos em etapas de preço para espelhar o auxiliar `toDigits` do código original. Quando o instrumento não fornece uma etapa de preço, é utilizado um valor padrão de 1.

## Diferenças da versão MQL
- O projeto MT5 permitiu prazos separados para as entradas do bloco. A porta StockSharp opera em um período de tempo definido por `CandleTimeFrame`, correspondendo ao uso comum do EA original (todos os blocos no período do gráfico).
- As paradas virtuais e o tratamento de ordens pendentes da biblioteca genérica de blocos não são necessários e, portanto, são omitidos.
- Os níveis protetores de stop-loss e take-profit são emulados monitorando velas e fechando a posição com `SellMarket` assim que um nível é violado.
- As decorações de registro e gráfico do ambiente MQL não são replicadas.

## Dicas de uso
- Otimize os tamanhos das linhas e os turnos do instrumento negociado. Os valores padrão imitam a predefinição original (três velas de baixa começando três barras atrás, seguidas por duas velas de alta começando uma barra atrás).
- Ajuste `StochasticLowerLevel` e `StochasticUpperLevel` para ajustar o quão restritivo o filtro do oscilador deve ser.
- Como o stop é baseado em mínimos recentes, instrumentos com grandes lacunas podem exigir a ampliação do lookback ou a adição de filtros adicionais.
