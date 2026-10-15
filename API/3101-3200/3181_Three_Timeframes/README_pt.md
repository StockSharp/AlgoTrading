# Estratégia de Three Timeframes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Three Timeframes** replica o expert do MetaTrader `Three timeframes.mq5` usando a API de alto nível do StockSharp. O sistema combina filtros de momentum e tendência obtidos de diferentes períodos:

- **MACD (M5)** detecta reversões de momentum recentes no período de trading.
- **Alligator (H4)** verifica que a estrutura do período superior está alinhada com a direção de trade pretendida.
- **RSI (H1)** confirma que o momentum no período intermediário apoia o rompimento.
- **Filtragem de sessão** opcional bloqueia trades fora dos horários de trabalho configurados.

A estratégia usa gerenciamento de risco baseado em pips. Níveis iniciais de stop-loss e take-profit são anexados a cada nova posição. Quando o preço avança, um trailing stop opcional ajusta o stop protetor após o mercado cobrir tanto a distância de trailing quanto o passo de trailing.

## Lógica de sinais
1. Os preços são processados em três assinaturas diferentes: velas de trading, velas de período superior para o Alligator e velas intermediárias para o RSI.
2. Uma configuração comprada requer:
   - Linha principal do MACD cruzando **abaixo** da linha de sinal na barra anterior enquanto a barra anterior a essa estava acima da linha de sinal, reproduzindo a regra do MetaTrader "azul cruza vermelho para baixo".
   - RSI no feed H1 acima de 50.
   - Alligator jaw > teeth > lips na vela H4 completada anterior, sinalizando uma estrutura de alta.
3. Uma configuração vendida replica as regras: a linha principal do MACD cruza acima da linha de sinal, RSI está abaixo de 50 e lips > teeth > jaw no Alligator para confirmar uma estrutura de baixa.
4. Se uma posição oposta existir, a estratégia a fecha enviando uma ordem a mercado pelo tamanho líquido, assim como o EA original antes de abrir um novo trade.
5. Após a entrada, a estratégia aplica as distâncias iniciais de stop-loss/take-profit e continua a fazer trailing do stop assim que o preço se move `TrailingStopPips + TrailingStepPips` a partir da entrada.

O filtro de sessão de trading reflete a implementação do MetaTrader. Quando a hora de início é menor que a hora de fim, o trading é permitido apenas dentro do intervalo. Quando a hora de início é maior que a hora de fim, a janela ativa abrange a meia-noite.

## Gestão de risco
- **Stop Loss / Take Profit** – as distâncias são expressas em pips. A estratégia os converte em unidades de preço usando o passo de preço do símbolo e ajusta para cotações FX de 3 ou 5 dígitos.
- **Trailing Stop** – ativa assim que o trade cobre tanto a distância do trailing stop quanto do trailing step. O stop é então movido para `price - trailing distance` para comprados e `price + trailing distance` para vendidos.
- **Volume de trading** – especifica o tamanho base de lote para novas ordens a mercado. A exposição oposta é zerada automaticamente antes de reverter.

## Diferenças em relação à versão do MetaTrader
- O modelo assíncrono de ordens do StockSharp elimina a necessidade de flags explícitos de rastreamento de transações (`m_waiting_transaction`). As ordens são executadas usando `BuyMarket`/`SellMarket`, que já aguardam confirmações internamente.
- As configurações de slippage, política de preenchimento e modo de margem da versão MQL são abstraídas pelo StockSharp. Esses controles específicos de plataforma não são necessários para a implementação .NET.
- O indicador Alligator é reconstruído a partir de médias móveis suavizadas preservando os períodos e deslocamentos originais. Os valores do indicador são armazenados em buffers deslizantes para reproduzir o comportamento de offset do Alligator integrado do MetaTrader.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da ordem a mercado em lotes/contratos. | `1` |
| `StopLossPips` | Distância inicial de stop-loss em pips. | `50` |
| `TakeProfitPips` | Distância inicial de take-profit em pips. | `140` |
| `TrailingStopPips` | Distância do trailing stop em pips. | `5` |
| `TrailingStepPips` | Movimento de pip adicional necessário antes de mover o trailing stop. | `5` |
| `MacdFastPeriod` | Comprimento de EMA rápida para MACD. | `13` |
| `MacdSlowPeriod` | Comprimento de EMA lenta para MACD. | `26` |
| `MacdSignalPeriod` | Período de suavização de sinal para MACD. | `10` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Períodos SMMA do Alligator para jaw/teeth/lips. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Deslocamentos para frente para as linhas do Alligator. | `8`, `5`, `3` |
| `RsiPeriod` | Comprimento de média do RSI no período intermediário. | `14` |
| `CandleType` | Período de trading (padrão velas de 5 minutos). | `M5` |
| `AlligatorCandleType` | Período superior para cálculo do Alligator (padrão velas de 4 horas). | `H4` |
| `RsiCandleType` | Período intermediário para confirmação do RSI (padrão velas de 1 hora). | `H1` |
| `UseTimeFilter` | Habilita o filtro de sessão. | `true` |
| `StartHour` | Hora de início da sessão (inclusiva). | `10` |
| `EndHour` | Hora de fim da sessão (exclusiva). | `15` |

## Notas de uso
- Certifique-se de que o instrumento selecionado forneça os três streams de velas configurados (M5, H1, H4 por padrão). O StockSharp solicitará automaticamente todas as assinaturas necessárias via `GetWorkingSecurities()`.
- A conversão de pips depende de `Security.PriceStep`. Instrumentos com tamanhos de tick incomuns podem precisar de ajuste manual dos parâmetros de stop/take.
- Os trailing stops requerem que tanto `TrailingStopPips` quanto `TrailingStepPips` sejam maiores que zero. Definir qualquer parâmetro como zero desabilita a lógica de trailing, consistente com o expert MQL.
