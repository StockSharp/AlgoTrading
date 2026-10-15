# Estratégia Exp XPeriodCandle X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp XPeriodCandle X2 recria o especialista original do MetaTrader usando a API de alto nível do StockSharp. A estratégia constrói candles sintéticos em dois períodos suavizando cada barra e comparando a abertura atrasada de uma janela de retrospectiva configurável com o último fechamento suavizado. A cor do candle do período superior define o viés de tendência, enquanto o período de trabalho aguarda transições de cor para acionar entradas e saídas. Proteções opcionais de stop-loss e take-profit replicam as entradas de gestão monetária do código-fonte.

## Como funciona
- **Detecção de tendência** – a assinatura do período superior suaviza os preços de abertura e fechamento com a média móvel selecionada. Cada candle completado compara seu fechamento suavizado com a abertura suavizada atrasada de `TrendPeriod` barras atrás. Um fechamento acima da abertura atrasada produz uma cor altista (0), enquanto um fechamento abaixo produz uma cor baixista (2). A cor armazenada em `TrendSignalBar` determina se a tendência global é comprada (`+1`), vendida (`-1`) ou neutra.
- **Lógica de entrada** – o período de trabalho aplica o mesmo suavizado. Para cada candle concluído, a estratégia armazena as cores atual e anterior referenciadas por `EntrySignalBar`. Um setup vendido aparece quando a tendência do período superior é baixista, a cor atual é 0 e a cor anterior é 2, espelhando o flip de sinal XPeriodCandle original. Um setup comprado requer que a tendência seja altista, a cor atual seja 2 e a cor anterior seja 0.
- **Gestão de posição** – interruptores configuráveis fecham posições em flips de tendência (`CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`) e em reversões de nível de entrada (`CloseLongOnEntrySignal`, `CloseShortOnEntrySignal`). Novos trades dimensionam `Volume + |Position|`, portanto um sinal oposto tanto sai quanto reverte como o especialista MQL.
- **Controles de risco** – distâncias opcionais de stop-loss e take-profit são expressas em passos de preço (`StopLossTicks`, `TakeProfitTicks`). São ativados apenas quando o booleano correspondente está habilitado.
- **Métodos de suavização** – as médias móveis do StockSharp são usadas em vez da biblioteca SmoothAlgorithms original. Os modos disponíveis são Simple, Exponential, Smoothed (SMMA), Weighted, Hull, Kaufman Adaptive e Jurik. Os parâmetros `TrendPhase` e `EntryPhase` afetam apenas a suavização Jurik e são limitados ao intervalo ±100.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `TrendCandleType` | Tipo de candle de período superior usado para o filtro de tendência. |
| `EntryCandleType` | Tipo de candle de período de trabalho usado para entradas. |
| `TrendPeriod` | Número de candles suavizados que definem a abertura atrasada no período de tendência. |
| `EntryPeriod` | Número de candles suavizados que definem a abertura atrasada no período de entrada. |
| `TrendLength` | Comprimento de suavização para candles sintéticos de período superior. |
| `EntryLength` | Comprimento de suavização para candles sintéticos de período de trabalho. |
| `TrendPhase` | Parâmetro de fase Jurik para o período de tendência (ignorado por outros tipos de suavização). |
| `EntryPhase` | Parâmetro de fase Jurik para o período de entrada (ignorado por outros tipos de suavização). |
| `TrendSignalBar` | Deslocamento usado para ler a cor do candle de tendência (`1` corresponde à barra mais recentemente fechada). |
| `EntrySignalBar` | Deslocamento usado para ler cores de entrada (`1` referencia a última barra fechada, `2` a anterior). |
| `TrendSmoothing` | Tipo de média móvel aplicada à suavização do período superior. |
| `EntrySmoothing` | Tipo de média móvel aplicada à suavização do período de trabalho. |
| `EnableLongEntries` | Permitir posições compradas quando aparecem condições altistas. |
| `EnableShortEntries` | Permitir posições vendidas quando aparecem condições baixistas. |
| `CloseLongOnTrendFlip` | Fechar posições compradas quando a tendência do período superior se torna baixista. |
| `CloseShortOnTrendFlip` | Fechar posições vendidas quando a tendência do período superior se torna altista. |
| `CloseLongOnEntrySignal` | Fechar posições compradas quando o período de entrada imprime uma cor baixista. |
| `CloseShortOnEntrySignal` | Fechar posições vendidas quando o período de entrada imprime uma cor altista. |
| `UseStopLoss` | Habilitar proteção de stop-loss medida em passos de preço. |
| `StopLossTicks` | Distância de stop-loss em passos de preço. |
| `UseTakeProfit` | Habilitar proteção de take-profit medida em passos de preço. |
| `TakeProfitTicks` | Distância de take-profit em passos de preço. |

## Notas
- A lógica de abertura atrasada armazena a abertura suavizada mais antiga dentro do período configurado, correspondendo ao buffer circular do indicador original.
- Quando `TrendCandleType` e `EntryCandleType` são iguais, apenas uma assinatura de candle é criada, mas a lógica de dupla cor ainda funciona.
- Garantir que `Volume` esteja configurado adequadamente; trades de reversão incluem automaticamente a posição absoluta atual para replicar o comportamento de manipulação de lotes do MetaTrader.
