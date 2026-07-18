# Estratégia Canal EA com Ordens a Limite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- **Origem**: convertida do expert do MetaTrader 5 `ChannelEA1.mq5`.
- **Propósito**: monitorar um canal de preço intradiário entre duas horas definidas pelo usuário e enfileirar ordens a limite ao final dessa janela.
- **Abordagem**: a estratégia rastreia os preços mais altos e mais baixos observados durante a sessão e coloca ordens a limite simétricas para negociar possíveis reversões de volta ao lado oposto do canal.

A estratégia é adequada para instrumentos que exibem reversão à média uma vez que um range diário é estabelecido. Por design ela funciona em contas de compensação: uma ordem de venda a limite executada fechará uma posição comprada existente antes de abrir uma nova vendida e vice-versa.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `BeginHour` | `1` | Hora (0-23) quando o rastreamento do range intradiário começa. A estratégia cancela ordens pendentes e fecha posições neste momento. |
| `EndHour` | `10` | Hora (0-23) quando o range acumulado é avaliado e novas ordens a limite são colocadas. Suporta sessões overnight: se `BeginHour > EndHour`, a sessão abrange a meia-noite. |
| `OrderVolume` | `1` | Volume aplicado a cada ordem pendente. |
| `CandleType` | Período de `1 hora` | Série de candles usada para construir o canal. Você pode mudar para qualquer período suportado pelo StockSharp. |

## Lógica de trading
1. **Tratamento de sessão**
   - A estratégia deriva os timestamps de início e fim da sessão dos parâmetros `BeginHour` e `EndHour` usando os timestamps dos candles. Quando `BeginHour > EndHour`, o fim é movido para o dia seguinte.
   - No primeiro candle terminado cujo tempo de fechamento atinge o limite de início, a estratégia cancela todas as ordens ativas, fecha a posição aberta e redefine as estatísticas da sessão.
2. **Construção do canal**
   - Apenas candles cujo tempo de abertura está dentro da janela de sessão contribuem para o range. A estratégia mantém o máximo corrente e o mínimo corrente para a sessão e conta o número de candles contribuintes.
   - São necessários pelo menos dois candles terminados para formar um range válido, refletindo o comportamento do expert MQL5 original (condição `n > 2`).
3. **Colocação de ordens ao final da sessão**
   - Quando um candle terminado cruza o limite de fim, a estratégia verifica que o range foi formado e que a mínima está estritamente abaixo da máxima.
   - Em seguida coloca duas ordens pendentes:
     - `BuyLimit` na mínima de sessão registrada com volume `OrderVolume`.
     - `SellLimit` na máxima de sessão registrada com o mesmo volume.
   - As ordens permanecem ativas até que a próxima sessão comece. Como a estratégia roda em conta de compensação, essas ordens servem tanto como entradas quanto saídas: por exemplo, o `SellLimit` fecha uma posição comprada existente na máxima de sessão antes de estabelecer uma nova vendida.
4. **Preparação da próxima sessão**
   - No próximo limite de início, a estratégia fecha quaisquer posições restantes e remove ordens pendentes sobrantes antes de medir o novo canal.

## Notas adicionais
- Nenhum stop-loss explícito é definido. O gerenciamento de risco deve ser controlado através do dimensionamento de posição, substituições manuais ou lógica protetora externa.
- A lógica usa apenas candles terminados (`CandleStates.Finished`) para se manter alinhada com o comportamento do EA original.
- Certifique-se de que o fuso horário do feed de dados e do servidor corresponde às suas expectativas, porque os limites de sessão são avaliados no horário da bolsa/local.
- Ao otimizar, considere tanto as horas de trading quanto a duração do candle; a estratégia é sensível à combinação porque o range registrado depende do período selecionado.
