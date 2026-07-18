# Estratégia de Divergência RSI & CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Divergência RSI & CCI** é uma conversão do expert advisor MetaTrader `RSI&CCI_DIVERGENCE.mq4` (MQL ID 22266). O sistema busca divergências baixistas ou altistas entre máximos de preço e dois osciladores (Commodity Channel Index e Relative Strength Index), os filtra com um filtro de tendência de média móvel linear ponderada, valida o sinal com alinhamento MACD em três marcos temporais diferentes e confirma a força do momentum usando um oscilador de momentum em um marco temporal superior. Alvos opcionais absolutos de stop-loss e take-profit podem ser aplicados para gerenciar posições abertas.

A implementação do StockSharp foca na API de alto nível. Os indicadores são vinculados diretamente às assinaturas de candles e todos os cálculos são impulsionados por atualizações de candles em streaming sem recuperação manual de valores do indicador.

## Lógica de negociação
1. **Filtro de tendência**
   - Médias móveis lineares ponderadas (LWMA) rápidas e lentas no período primário definem a direção prevalecente.
   - O contexto altista requer que a LWMA rápida esteja acima da LWMA lenta; o contexto baixista requer o oposto.

2. **Detecção de divergência**
   - O último candle fechado é comparado com até `CandlesToRetrace` candles anteriores.
   - Um sinal altista ocorre se CCI ou RSI faz uma mínima mais alta enquanto o candle anterior correspondente mostra uma máxima mais alta do que a última máxima (divergência altista).
   - Um sinal baixista ocorre se CCI ou RSI faz uma máxima mais baixa enquanto o candle anterior correspondente mostra uma máxima mais baixa do que a última máxima (divergência baixista).

3. **Confirmação MACD**
   - O MACD (12, 26, 9 por padrão) é avaliado nos períodos primário, superior e macro.
   - Operações compradas requerem que o MACD esteja acima da linha de sinal em todos os períodos.
   - Operações vendidas requerem que o MACD esteja abaixo da linha de sinal em todos os períodos.

4. **Confirmação de momentum**
   - Um oscilador de momentum (comprimento 14 por padrão) é amostrado em um período superior (padrão 1 hora).
   - O desvio absoluto das leituras recentes de momentum do nível neutro 100 deve exceder os limiares de compra/venda configurados para aprovar a operação.

5. **Guarda de estrutura de preço**
   - A estratégia verifica máximas/mínimas recentes para imitar as restrições do EA original (`Low[2] < High[1]` para comprados e `Low[1] < High[2]` para vendidos).

6. **Execução de ordens**
   - Quando todos os filtros se alinham, a estratégia entra usando `BuyMarket` ou `SellMarket` com volume igual ao volume base da estratégia mais o valor absoluto da posição atual, permitindo reversão imediata.

7. **Gestão de risco**
   - Distâncias absolutas opcionais de stop-loss e take-profit são avaliadas em cada candle finalizado.
   - Se configuradas, a estratégia envia uma ordem de mercado dimensionada para liquidar a posição quando o stop ou o alvo é tocado.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `FastMaLength` | 6 | Período para o filtro de tendência LWMA rápida. |
| `SlowMaLength` | 85 | Período para o filtro de tendência LWMA lenta. |
| `CciLength` | 14 | Período de retrospectiva para o Commodity Channel Index. |
| `RsiLength` | 14 | Período de retrospectiva para o Relative Strength Index. |
| `CandlesToRetrace` | 10 | Número de candles concluídos usados para detectar divergências. |
| `MacdFastPeriod` | 12 | Período de média móvel rápida no cálculo MACD. |
| `MacdSlowPeriod` | 26 | Período de média móvel lenta no cálculo MACD. |
| `MacdSignalPeriod` | 9 | Período da linha de sinal para MACD. |
| `MomentumLength` | 14 | Comprimento do oscilador de momentum no período superior. |
| `MomentumBuyThreshold` | 0.3 | Desvio absoluto mínimo de 100 para confirmação de momentum altista. |
| `MomentumSellThreshold` | 0.3 | Desvio absoluto mínimo de 100 para confirmação de momentum baixista. |
| `StopLoss` | 0 | Distância absoluta de preço para um stop-loss opcional (0 desabilita o stop). |
| `TakeProfit` | 0 | Distância absoluta de preço para um take-profit opcional (0 desabilita o alvo). |
| `CandleType` | Período de 15 minutos | Tipo de candle primário para análise de divergência e tendência. |
| `MomentumCandleType` | Período de 1 hora | Tipo de candle usado para a confirmação de momentum. |
| `HigherMacdCandleType` | Período de 1 hora | Período secundário para confirmação MACD. |
| `MacroMacdCandleType` | Período de 30 dias | Período macro para confirmação MACD (ajustar para corresponder à disponibilidade de dados do instrumento). |

## Notas de uso
- Certifique-se de que todos os períodos referenciados estejam disponíveis no provedor de dados; caso contrário, ajuste os parâmetros de tipo de candle adequadamente.
- Os valores padrão de stop-loss e take-profit estão desabilitados para refletir o comportamento original do EA onde o risco era gerenciado via stops de trailing e equidade. Defina valores decimais positivos para habilitar stops rígidos.
- Como a confirmação de momentum compara valores com a linha de base 100, assume que o indicador `Momentum` do StockSharp usa a definição clássica (`100 * Close / Close[N]`). Se uma normalização diferente for preferida, ajuste os limiares para corresponder à volatilidade do instrumento.
- A estratégia envia ordens de mercado tanto para entradas quanto para saídas, refletindo a lógica de execução imediata do expert advisor fonte.

## Notas de conversão
- A conversão usa o vínculo de indicadores de alto nível do StockSharp. Nenhuma chamada manual a `GetValue` é necessária; os valores do indicador são fornecidos pelos callbacks de vínculo.
- O gerenciamento de stop baseado em equidade, lógica de trailing e recursos de e-mail/notificação da fonte MQL não são portados. Em vez disso, o foco é colocado na geração de sinais primária e no tratamento básico de stop/alvo.
- A detecção de divergência é implementada usando listas leves para manter o histórico recente de preço e indicadores necessário para o reconhecimento de padrões.
