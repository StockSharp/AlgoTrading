# Estratégia de Filtro de Momentum Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert advisor **ENGULFING** do MetaTrader para a API de alto nível do StockSharp. Combina um padrão de candle envolvente altista/baixista no período de trabalho com confirmação de momentum de período superior e um filtro de tendência MACD mensal. O gerenciamento de risco reproduz o comportamento original de break-even e trailing usando distâncias de stop medidas em passos do instrumento.

## Como funciona

1. **Padrão de candles** – o último candle terminado deve engolir a barra anterior na direção da operação. A estratégia também verifica que a barra de dois períodos atrás se sobrepõe à barra anterior, espelhando a confirmação baseada em fractais do original.
2. **Filtro de tendência** – médias móveis *ponderadas* rápida e lenta (análogo de LWMA) controlam as entradas. Operações compradas requerem que a média rápida esteja acima da lenta e vice-versa para as vendidas.
3. **Filtro de momentum** – um indicador de momentum de 14 períodos calculado em um período superior deve se desviar do nível neutro (100) pelo menos o limiar configurado em qualquer um dos últimos três valores. Isso reproduz as verificações `MomLevelB/MomLevelS` do código MQL.
4. **Filtro MACD** – uma série MACD mensal (30 dias) deve mostrar a linha principal acima da linha de sinal para posições compradas e abaixo para vendidas, assim como a comparação `MacdMAIN0` vs `MacdSIGNAL0` no EA.
5. **Tratamento de ordens** – a estratégia sempre vira a posição quando um sinal oposto aparece. A lógica de proteção fecha operações sempre que as regras de stop, alvo, break-even ou trailing acionam.

## Gerenciamento de risco

- **Stop Loss / Take Profit** – as distâncias são configuradas em passos do instrumento (ticks). Elas espelham as entradas `Stop_Loss` e `Take_Profit` do EA original.
- **Trailing Stop** – trailing opcional medido em passos. O stop acompanha o melhor preço alcançado após a entrada.
- **Movimento Break-Even** – uma vez que o preço avança `BreakEvenTriggerSteps`, o stop é movido para a entrada mais `BreakEvenOffsetSteps`, reproduzindo o recurso "sem perda" (`USEMOVETOBREAKEVEN`).

Os alvos baseados em dinheiro do script MQL (`Use_TP_In_Money`, `Take_Profit_In_percent`) são intencionalmente omitidos para manter a lógica consistente com o sistema de unidades do StockSharp. Saídas baseadas em porcentagem ou moeda podem ser recriadas ajustando os parâmetros de stop/take.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos das médias móveis ponderadas usadas para confirmação de tendência. |
| `MomentumPeriod` | Comprimento do Momentum no período superior. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desvio absoluto mínimo de 100 exigido para o filtro de momentum. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configuração MACD aplicada a `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Distâncias de stop protetor e alvo em passos de preço. Definir como zero para desativar. |
| `TrailingStopSteps` | Distância opcional do trailing stop (0 desativa o trailing). |
| `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Distância necessária antes de mover o stop para break-even e o offset aplicado. |
| `CandleType` | Período principal onde os padrões envolventes são avaliados. |
| `HigherCandleType` | Período superior usado para o filtro de momentum (padrão: 1 hora). |
| `MacdCandleType` | Período para o filtro de tendência MACD (padrão: 30 dias ≈ mensal). |

## Uso

1. Anexar a estratégia a um instrumento e definir `CandleType`, `HigherCandleType` e `MacdCandleType` para corresponder aos períodos preferidos.
2. Ajustar os parâmetros de MA e momentum se quiser alinhar com uma estrutura de mercado diferente.
3. Configurar as distâncias de stop, take profit, trailing e break-even em passos de preço que correspondam ao tamanho de tick do instrumento.
4. Iniciar a estratégia; ela assinará automaticamente todos os feeds de candles necessários e começará a avaliar sinais uma vez que os indicadores estejam formados.

## Notas e diferenças em relação ao EA original

- Médias móveis ponderadas replicam os cálculos LWMA usados em MQL sem iterar manualmente sobre preços.
- A lógica de break-even e trailing é aplicada em candles completados, correspondendo à abordagem barra por barra do EA enquanto aproveita os helpers de proteção do StockSharp.
- Trailing baseado em dinheiro e saídas baseadas em porcentagem não são portados porque o StockSharp opera em unidades do instrumento; comportamento equivalente pode ser alcançado calibrando os parâmetros baseados em passos.
- A estratégia assume uma posição por vez, que é o cenário de uso comum do EA fonte embora ele expusesse uma entrada `Max_Trades`.

Ajuste os limiares e períodos para corresponder ao ativo que está sendo negociado. Instrumentos de maior volatilidade frequentemente requerem distâncias de passo maiores ou limiares de momentum mais amplos para evitar saídas prematuras.
