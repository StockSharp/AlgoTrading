# Estratégia Little EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Little EA é um expert de cruzamento de médias móveles originalmente escrito para o MetaTrader. A estratégia observa o candle selecionado pelo parâmetro **OHLC bar index** e reage quando esse candle cruza uma média móvel deslocada de baixo para cima ou de cima para baixo. O port StockSharp mantém a ideia original de múltiplas entradas ao permitir várias tranches por direção, respeitando uma exposição máxima configurável.

## Lógica de trading
1. Assinar a série de candles configurada e alimentar o tipo de média móvel selecionado com a fonte de preço escolhida (fechamento, abertura, máxima, mínima, mediana, típico ou ponderado).
2. Armazenar candles completados para que a estratégia possa referenciar o candle no `OhlcBarIndex` (o valor padrão `1` significa o último candle completamente fechado).
3. Aplicar o `MaShift` opcional lendo o valor da média móvel de várias barras atrás, replicando o deslocamento visual do MetaTrader.
4. Quando o candle de referência fecha acima da MA deslocada, tratar como cruzamento altista. Quando fecha abaixo, tratar como cruzamento baixista.
5. Para um cruzamento altista:
   - Se a exposição vendida líquida já iguala o máximo configurado, fechar toda a posição vendida.
   - Caso contrário, se a exposição comprada ainda está abaixo do máximo, adicionar uma tranche de `TradeVolume` ao lado comprado.
6. Para um cruzamento baixista:
   - Se a exposição comprada já iguala o máximo, fechar toda a posição comprada.
   - Caso contrário, se a exposição vendida está abaixo do limite, adicionar uma tranche de `TradeVolume` ao lado vendido.

O limite de volume emula o limite `Int_Max_Pos` do expert original enquanto trabalha com as posições líquidas do StockSharp.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Período principal usado para sinais e cálculos de indicadores. |
| `OhlcBarIndex` | `int` | `1` | Índice do candle histórico usado para detecção de cruzamento (0 = candle atual em formação, 1 = último candle terminado). |
| `MaxPositionsPerSide` | `int` | `15` | Número máximo de tranches de `TradeVolume` que podem ser acumuladas por direção. |
| `MaPeriod` | `int` | `64` | Comprimento da média móvel. |
| `MaShift` | `int` | `0` | Número de barras para deslocar a MA para trás ao verificar cruzamentos. |
| `MaType` | `MovingAverageType` | `Smoothed` | Modo de cálculo da média móvel (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceType` | `Close` | Fonte de preço usada como entrada do indicador. |
| `TradeVolume` | `decimal` | `1` | Volume de ordem enviado com cada nova tranche. |

## Diferenças em relação ao expert original do MetaTrader
- A gestão de dinheiro está simplificada: apenas entradas de volume fixo são suportadas. O dimensionamento de risco percentual do EA original não está implementado.
- O StockSharp trabalha com posições líquidas, então posições em direção oposta são fechadas antes de nova exposição ser acumulada. O limite de `MaxPositionsPerSide` é aplicado à exposição líquida em lotes.
- Os valores do indicador e o histórico de candles são processados através da API de assinatura de candles de alto nível em vez de cópias manuais de buffer.

## Dicas de uso
- Ajustar `TradeVolume` para corresponder ao passo de lote do instrumento antes de lançar a estratégia; o construtor também atribui o mesmo valor a `Strategy.Volume` para que os métodos auxiliares usem o tamanho desejado por padrão.
- Usar `MaShift` em combinação com `OhlcBarIndex` para recriar o alinhamento visual do gráfico MetaTrader se necessário.
- Adicionar a estratégia a um gráfico para ver candles, a sobreposição da média móvel e as operações executadas, o que ajuda a verificar o comportamento de cruzamento.

## Indicadores
- Uma média móvel configurável (`Simple`, `Exponential`, `Smoothed` ou `Weighted`).
