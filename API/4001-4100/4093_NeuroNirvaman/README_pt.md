# Estratégia Neuro Nirvaman MQ4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Neuro Nirvaman MQ4** é uma versão fiel do MetaTrader 4 consultor especialista `NeuroNirvaman.mq4`. O robô original combina um filtro Laguerre personalizado aplicado ao componente +DI do indicador ADX com um detector de fuga SilverTrend. Três perceptrons avaliam esses insumos e um supervisor decide se compra ou vende. A versão StockSharp espelha esse fluxo de trabalho e executa uma posição por vez, recalculando sua lógica apenas em velas totalmente fechadas.

## Como funciona a estratégia
1. **Feed de dados de mercado** – A estratégia assina uma única série de velas definida por `CandleType` e processa apenas `Finished` velas. Não avalia eventos intrabar, replicando a verificação `Time[0]` usada no MT4.
2. **Suavização Laguerre +DI** – Quatro indicadores `AverageDirectionalIndex` fornecem valores +DI que são enviados através de um filtro Laguerre (`LaguerrePlusDiState`) usando a gama original de 0,764. O filtro produz valores de oscilador no intervalo `[0, 1]` e cada fluxo tem seu próprio período ADX e largura de zona neutra (`Laguerre*Distance`).
3. **Porta SilverTrend** – Dois objetos `SilverTrendState` reproduzem a lógica `Sv2.mq4`. Eles rastreiam a máxima mais alta e a mínima mais baixa para velas `SSP`, reduzem o canal com a constante `Kmax = 50.6` e retornam `1` para uma tendência de alta ou `-1` para uma tendência de baixa. As profundidades de lookback são controladas por `SilverTrend1Length` e `SilverTrend2Length`.
4. **Perceptrons** –
   - *Perceptron #1* mistura a primeira ativação de Laguerre com o primeiro swing SilverTrend usando pesos `X11 - 100` e `X12 - 100`.
   - *Perceptron #2* combina a segunda ativação de Laguerre com o segundo swing SilverTrend e pesos `X21 - 100` e `X22 - 100`.
   - *Perceptron #3* avalia a terceira e quarta ativações de Laguerre ponderadas por `X31 - 100` e `X32 - 100`.
Cada ativação de Laguerre é quantizada em `-1`, `0` ou `1` dependendo de sua distância do nível de equilíbrio 0,5.
5. **Supervisor (`Pass`)** – O supervisor reproduz a função MQL `Supervisor()`:
   - `Pass = 3`: requer `Perceptron #3 > 0`. Se também `Perceptron #2 > 0`, a estratégia compra usando o segundo conjunto TP/SL; caso contrário, se `Perceptron #1 < 0`, ele vende usando o primeiro conjunto TP/SL.
   - `Pass = 2`: um `Perceptron #2` positivo abre uma posição longa com o segundo conjunto TP/SL, enquanto qualquer valor não positivo abre uma posição curta com o primeiro conjunto.
   - `Pass = 1`: um `Perceptron #1` negativo abre uma posição curta, caso contrário, uma posição longa é aberta. Ambas as filiais usam o primeiro conjunto TP/SL.
6. **Gerenciamento de pedidos e riscos** – As inscrições são enviadas com `BuyMarket` ou `SellMarket` usando `TradeVolume`. Os níveis de take-profit e stop-loss são calculados como `entry ± points * PriceStep`. Como StockSharp coloca ordens de mercado puras, as saídas de proteção são simuladas verificando os máximos e mínimos das velas, exatamente como as ordens TP/SL do lado da corretora seriam acionadas no MT4.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 15 minutos | Tipo de vela processado pela estratégia. |
| `TradeVolume` | `decimal` | 0,1 | Volume do pedido em lotes. |
| `SilverTrend1Length` | `int` | 7 | Comprimento de lookback para o primeiro cálculo SilverTrend (SSP). |
| `Laguerre1Period` | `int` | 14 | ADX período para a primeira transmissão de Laguerre. |
| `Laguerre1Distance` | `decimal` | 0 | Largura da zona neutra (porcentagem) em torno de 0,5 para o riacho Laguerre #1. |
| `X11`, `X12` | `decimal` | 100 | Pesos usados dentro do perceptron #1 (o código subtrai 100 antes de aplicá-los). |
| `TakeProfit1`, `StopLoss1` | `decimal` | 100/50 | Distâncias de proteção em pontos para o primeiro perfil de risco e todas as negociações curtas. |
| `SilverTrend2Length` | `int` | 7 | Comprimento de lookback para o segundo cálculo do SilverTrend. |
| `Laguerre2Period` | `int` | 14 | Período ADX para a segunda transmissão do Laguerre. |
| `Laguerre2Distance` | `decimal` | 0 | Largura da zona neutra (porcentagem) em torno de 0,5 para o riacho Laguerre #2. |
| `X21`, `X22` | `decimal` | 100 | Pesos usados dentro do perceptron #2. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 100/50 | Distâncias de proteção em pontos para o segundo perfil de risco. |
| `Laguerre3Period`, `Laguerre4Period` | `int` | 14 | ADX períodos para o terceiro e quarto fluxos de Laguerre. |
| `Laguerre3Distance`, `Laguerre4Distance` | `decimal` | 0 | Larguras da zona neutra (porcentagem) para o terceiro e quarto riachos Laguerre. |
| `X31`, `X32` | `decimal` | 100 | Pesos usados dentro do perceptron #3. |
| `Pass` | `int` | 3 | Ramo supervisor que seleciona quais perceptrons podem desencadear negociações. |

## Notas de uso
- Os pesos padrão de `100` neutralizam a entrada do perceptron correspondente. Afaste os pesos de 100 para criar sinais significativos.
- SilverTrend começa a retornar `±1` assim que velas suficientes são coletadas. Até então, as saídas do perceptron podem permanecer em zero, emulando o comportamento do MT4 onde `iCustom` retorna zero antes que os buffers estejam prontos.
- As verificações de take-profit e stop-loss dependem dos extremos das velas; se ocorrerem picos intra-candle entre as barras, a simulação pode divergir ligeiramente da execução do lado da corretora.
- Apenas uma posição pode existir por vez. Um novo sinal é ignorado até que a posição atual seja fechada por TP, SL ou por uma decisão oposta.
- Ajuste `CandleType` para espelhar o período do gráfico usado pela configuração original do MT4 (por exemplo, M15 ou H1) para manter a escala do indicador consistente.
