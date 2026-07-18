# Rompimento de Bollinger DC2008
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reimplementação do consultor especialista de rompimento de Bollinger de Sergey Pavlov (DC2008) do MetaTrader para a API de estratégia de alto nível do StockSharp. A estratégia observa candles concluídos, avalia rompimentos das Bandas de Bollinger na fonte de preço selecionada e abre ou reverte posições apenas quando a operação atual não está em prejuízo.

## Visão geral
- Calcula um envelope de Bandas de Bollinger no timeframe configurado e preço aplicado (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado ou médio).
- Gera configurações de **comprado** quando o mínimo do candle fecha abaixo da banda inferior enquanto o máximo permanece abaixo da banda média (forte extensão de baixa que deveria reverter).
- Gera configurações de **vendido** quando o máximo do candle excede a banda superior enquanto o mínimo permanece acima da banda média (forte extensão de alta esperada para reverter).
- O expert MQL original operava em ticks; neste port os sinais são processados uma vez por candle terminado para maior estabilidade e coerência do indicador.
- As posições só são abertas ou revertidas se a posição existente mostrar um lucro não realizado não negativo, replicando o filtro de risco original.

## Lógica de trading
### Pipeline do indicador
1. Assinar candles do `CandleType` escolhido (padrão: timeframe de 1 hora).
2. Alimentar o preço aplicado selecionado no indicador de Bandas de Bollinger (`Length = BandsPeriod`, `Width = BandsDeviation`).
3. Ignorar candles até que o indicador produza valores válidos de banda superior, média e inferior.

### Critérios de entrada
- **Comprar**: `Low < LowerBand` **e** `High < MiddleBand`. Indica que todo o candle operou abaixo da linha média após perfurar a banda inferior.
- **Vender**: `High > UpperBand` **e** `Low > MiddleBand`. Indica que todo o candle operou acima da linha média após perfurar a banda superior.

### Filtro de posição e gestão
- Se **não houver posição**, a estratégia abre uma ordem de mercado com o `Volume` configurado quando um sinal aparece.
- Se já existir uma posição:
  - Quando o sinal é oposto à direção atual, calcular o lucro não realizado como `Position * (Close - PositionPrice)` usando o fechamento do candle.
  - Se o lucro não realizado for **negativo**, pular todas as ações para este candle (idêntico ao `return` antecipado do original).
  - Se o lucro não realizado for **não negativo** e o sinal for oposto, enviar uma ordem de mercado de reversão de tamanho `Volume + |Position|` para tanto liquidar a posição atual quanto estabelecer uma nova na direção do sinal.
  - Sinais que correspondem à direção atual não adicionam à posição (igual à versão MQL).
- Não há ordens explícitas de stop-loss ou take-profit; saídas de operações ocorrem apenas mediante sinais opostos que satisfazem o filtro de lucro.

## Parâmetros
| Nome | Valor padrão | Descrição |
| --- | --- | --- |
| `BandsPeriod` | 80 | Número de candles usados para calcular a média móvel e desvios de Bollinger. Deve ser positivo e está disponível para otimização. |
| `BandsDeviation` | 3.0 | Multiplicador de desvio padrão aplicado à largura das Bandas de Bollinger. Positivo, otimizável. |
| `AppliedPrice` | Close | Fonte de preço para o indicador: Close, Open, High, Low, Median, Typical, Weighted ou Average (OHLC/4). Espelha `ENUM_APPLIED_PRICE` do MetaTrader. |
| `CandleType` | Timeframe de 1 hora | Tipo de candle (timeframe) usado para análise e ordens. Pode ser trocado por qualquer outro tipo de dado suportado pelo StockSharp. |
| `Volume` (herdado) | dependente do broker | Tamanho de ordem para novas entradas. Reversões adicionam automaticamente o tamanho absoluto da posição existente. |

## Diferenças em relação ao expert MQL original
- O EA MetaTrader avaliava condições a cada tick; este port C# aguarda candles terminados para evitar agir sobre dados incompletos.
- O deslocamento do indicador estava fixo em zero no EA fonte e permanece implícito aqui; deslocamentos adicionais não são expostos.
- O MetaTrader reportava o lucro flutuante diretamente; o port o aproxima via fechamento do candle e `PositionPrice`, o que é suficiente para a comparação de sinal usada pelo filtro.
- Gestão de operações, mensagens de texto e comentários de ordens da versão MQL são omitidos, focando puramente na geração de sinais.

## Notas de implementação
- Candles, indicadores e chamadas de trading dependem das APIs de alto nível do StockSharp (`SubscribeCandles().Bind(...)`, `BuyMarket`, `SellMarket`).
- O indicador é desenhado automaticamente se uma área de gráfico estiver disponível na UI; as operações também são representadas para depuração.
- A estratégia reinicia e reconstrói o indicador a cada início, portanto as alterações de parâmetros têm efeito imediato na próxima execução.
