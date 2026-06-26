# Estratégia Exp Spearman Rank Correlation Histogram
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia do StockSharp porta o especialista MetaTrader **Exp_SpearmanRankCorrelation_Histogram**. Assina uma série de velas configurável, calcula o histograma de correlação de ranque de Spearman para cada barra concluída e reage quando o estado codificado por cor muda. Dependendo do modo de negociação, o algoritmo pode fechar posições opostas, reverter para um novo trade ou aguardar valores extremos antes de agir.

## Pipeline do indicador

1. Um indicador `RankCorrelationIndex` (correlação de ranque de Spearman escalada para ±100) é alimentado com os preços de fechamento das velas. A janela de lookback é limitada por `MaxRange` e o padrão é de 14 barras.
2. A correlação bruta é normalizada para o intervalo `[-1, 1]`. Quando `InvertCorrelation` está habilitado, o sinal é invertido para emular o flag `direction` do MQL.
3. O valor normalizado é comparado com `HighLevel` e `LowLevel` para atribuir um estado de cor:
   * `4` – zona fortemente de alta (`value > HighLevel`).
   * `3` – zona moderadamente de alta (`0 < value ≤ HighLevel`).
   * `2` – neutral (`value == 0`).
   * `1` – zona moderadamente de baixa (`LowLevel ≤ value < 0`).
   * `0` – zona fortemente de baixa (`value < LowLevel`).
4. As cores mais recentes são armazenadas em um buffer estilo série para que o índice `0` represente a vela fechada mais recente, o índice `1` a anterior, e assim por diante.

## Fluxo de trabalho de trading

* Os sinais são avaliados apenas em velas concluídas (`CandleStates.Finished`).
* O parâmetro `SignalBar` define qual barra concluída é inspecionada (padrão uma barra atrás). A estratégia também olha para a barra imediatamente mais antiga, replicando a pesquisa de buffer duplo do consultor especialista.
* Os interruptores de ordem (`AllowBuyEntries`, `AllowSellEntries`, `AllowBuyExits`, `AllowSellExits`) decidem se posições longas/curtas podem ser abertas ou fechadas.
* Os modos de negociação reproduzem o interruptor do MetaTrader:
  * **Modo 1** – fechar a posição oposta sempre que a cor mais antiga for de alta/baixa (`> 2` ou `< 2`). Se permitido, abrir na nova direção quando a cor recente sair da zona de alta (`< 3`) ou baixa (`> 1`).
  * **Modo 2** – reagir apenas a cores extremas. O extremo de alta (`4`) permite à estratégia fechar posições curtas e opcionalmente abrir longas quando a barra mais nova cair abaixo de `4`. O extremo de baixa (`0`) fecha longas e pode abrir curtas quando a barra mais nova subir acima de `0`.
  * **Modo 3** – uma versão mais estrita do Modo 2: posições curtas são fechadas imediatamente em `4`, longas em `0`, e novos trades são permitidos sob as mesmas condições que o Modo 2.
* `CancelActiveOrders()` é executado antes de enviar novas ordens de mercado para evitar solicitações obsoletas.
* Reversões de posição usam o `Volume` configurado mais a posição atual absoluta para que o trade mude completamente para o lado oposto.
* `StopLossPoints` e `TakeProfitPoints` opcionais (unidades de preço) habilitam gerenciamento de risco baseado em `StartProtection`; quando deixados em `0`, nenhuma ordem de proteção é gerada.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período de tempo usado para o indicador e decisões de trading. |
| `RangeLength` | Período de lookback nominal de Spearman (limitado por `MaxRange`). |
| `MaxRange` | Limite superior para o comprimento de lookback efetivo; cai para `10` se definido como `0`. |
| `HighLevel`, `LowLevel` | Limiares que separam zonas de alta e baixa do histograma. |
| `SignalBar` | Número de barras fechadas para pular antes de analisar o histograma. |
| `InvertCorrelation` | Inverte o sinal do histograma para corresponder ao comportamento `direction=false` do MQL. |
| `AllowBuyEntries`, `AllowSellEntries` | Habilitar abertura de posições longas/curtas. |
| `AllowBuyExits`, `AllowSellExits` | Habilitar fechamento automático de posições longas/curtas existentes. |
| `TradeMode` | Seleciona a lógica do Modo 1, Modo 2 ou Modo 3 do especialista original. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção opcionais em unidades de preço absolutas para `StartProtection`. |
| `Volume` (integrado) | Tamanho base de ordem usado ao abrir ou reverter posições. |

## Diferenças do especialista MetaTrader

* Entradas de gestão de capital (`MM`, `MMMode`) e slippage (`Deviation_`) não são replicados; o dimensionamento de posição depende da propriedade padrão `Volume` e da configuração do corretor.
* As funções auxiliares MQL de `TradeAlgorithms.mqh` são substituídas por chamadas diretas a `BuyMarket`/`SellMarket` após cancelar ordens pendentes.
* A dica de desempenho `CalculatedBars` é desnecessária no StockSharp e foi omitida.
* O flag `direction` é representado por `InvertCorrelation`, que simplesmente espelha o sinal do histograma.
* As distâncias de stop-loss e take-profit (`StopLoss_`, `TakeProfit_`) são interpretadas como offsets de preço absolutos ao habilitar `StartProtection`; nenhuma conversão automática de ponto para preço é realizada.
* Os tempos de sinal são tratados no fechamento da vela; não há agendamento diferido para a abertura da próxima barra.

Esses ajustes seguem as diretrizes de estratégia de alto nível do StockSharp enquanto preservam a lógica de sinal original.
