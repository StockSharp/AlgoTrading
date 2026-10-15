# Estratégia de Área MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Área MACD avalia o equilíbrio entre o momentum altista e baixista usando a linha principal do MACD. Para cada candle, a estratégia acumula a soma de todos os valores positivos do MACD e a soma absoluta de todos os valores negativos do MACD ao longo de uma janela de histórico configurável. O lado dominante define a direção de trading: uma área positiva mais forte favorece posições compradas, enquanto uma área negativa mais forte favorece exposição vendida. Um interruptor reverso permite negociar contra a tendência detectada quando necessário.

A implementação usa a API de alto nível do StockSharp com assinaturas de candles e vínculos de indicadores. Apenas candles concluídos são processados, e toda a lógica de trading está encapsulada dentro do manipulador `ProcessCandle`.

## Indicadores e Dados
- **MACD (Convergência/Divergência de Médias Móveis)** com períodos rápido, lento e de sinal configuráveis.
- **Candles** de um período definido pelo usuário (30 minutos por padrão).

## Regras de Trading
1. **Entrada Comprada** – Quando a área positiva acumulada do MACD é maior que a área negativa absoluta acumulada. Se o modo reverso estiver habilitado, a condição é invertida.
2. **Entrada Vendida** – Quando a área negativa absoluta acumulada do MACD domina. O modo reverso troca o comportamento.
3. **Gestão de Posição** – Quando um novo sinal de entrada aparece, a estratégia fecha qualquer posição oposta antes de abrir a nova para que apenas uma única posição direcional seja mantida.

## Gestão de Risco
- **Stop Loss** – Distância fixa em pips medida a partir do preço de entrada. Convertida automaticamente em unidades de preço usando o passo de preço do instrumento.
- **Take Profit** – Alvo de lucro fixo em pips usando as mesmas regras de conversão.
- **Trailing Stop** – Stop de acompanhamento opcional que é ativado quando a posição se move em lucro por `TrailingStopPips + TrailingStepPips`. O stop então segue o preço com uma lacuna definida por `TrailingStopPips` e apenas avança quando o preço avança pelo menos `TrailingStepPips` a mais. Ambos os valores devem ser maiores que zero para habilitar a lógica de trailing.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume de ordem usado para entradas de mercado. | 1 |
| `HistoryLength` | Número de candles armazenados para a comparação de área MACD. | 60 |
| `MacdFastLength` | Período de EMA rápida para o MACD. | 12 |
| `MacdSlowLength` | Período de EMA lenta para o MACD. | 26 |
| `MacdSignalLength` | Período de EMA de sinal para o MACD. | 9 |
| `ReverseSignals` | Se habilitado, troca as condições de entrada comprada e vendida. | false |
| `StopLossPips` | Distância de stop loss expressa em pips. | 100 |
| `TakeProfitPips` | Distância de take profit em pips. | 150 |
| `TrailingStopPips` | Distância do trailing stop em pips. Definir como zero para desabilitar o trailing. | 5 |
| `TrailingStepPips` | Progresso adicional necessário antes de atualizar o trailing stop. Definir como zero para desabilitar o trailing. | 5 |
| `CandleType` | Período de candle usado pela assinatura. | Período de 30 minutos |

## Notas de Uso
1. Anexar a estratégia a um portfólio e um instrumento, depois ajustar os parâmetros para o mercado alvo.
2. Garantir que tanto `TrailingStopPips` quanto `TrailingStepPips` sejam maiores que zero para habilitar a proteção de trailing. Caso contrário, o trailing é ignorado e apenas os níveis de stop loss/take profit estão ativos.
3. Monitorar as mensagens de log para informações sobre eventos de stop-loss, take-profit e trailing. Todos os logs são produzidos em inglês conforme exigido.

## Ideia Original
A conversão é baseada no consultor especialista MetaTrader 5 "Area MACD". A versão StockSharp mantém o conceito central de comparar áreas MACD enquanto integra gestão de risco e manuseio de indicadores através da API de alto nível do framework.
