# Estratégia Neuro Nirvaman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Neuro Nirvaman é uma conversão direta do assessor especialista MetaTrader 5 *NeuroNirvamanEA*. Ela recria a árvore de decisão baseada em perceptron da implementação MQL original combinando quatro indicadores de direção positiva (+DI) suavizados por Laguerre com dois detectores de swing SilverTrend. A estratégia trabalha com velas terminadas e envia ordens de mercado com níveis dinâmicos de take-profit e stop-loss definidos em pontos. Nenhum trailing stop, averaging ou piramidação é aplicado – apenas uma única posição pode existir a qualquer momento.

## Entradas de mercado e indicadores
- **AverageDirectionalIndex (4 instâncias)** – cada instância é configurada com seu próprio período. A estratégia lê o componente +DI e o passa por um filtro de Laguerre para obter valores de oscilador suaves no intervalo `[0, 1]`.
- **LaguerrePlusDiState** – um helper interno que reproduz a lógica do indicador personalizado `laguerre_plusdi.mq5`, incluindo a suavização Laguerre de quatro etapas e a normalização `CU / (CU + CD)`.
- **SilverTrendState (2 instâncias)** – um porto fiel da lógica `silvertrend_signal.mq5`. Avalia as últimas 10 velas (`SSP = 9`) para detectar pontos de rompimento, e emite `1` em setas baixistas, `-1` em setas altistas, ou `0` quando nenhuma seta está presente.
- **Stream de velas** – a estratégia subscreve a um único período selecionado via `CandleType` e processa apenas velas terminadas.

## Lógica de trading
1. **Preparação de sinais**
   - Cada valor de Laguerre é traduzido em uma ativação discreta via o helper `ComputeTensionSignal`: valores acima de `0.5 + distance/100` geram `-1`, abaixo de `0.5 - distance/100` geram `1`, e a zona neutra produz `0`.
   - Os swings de SilverTrend são atualizados em cada vela. Os parâmetros de risco (`Risk1`, `Risk2`) encolhem ou alargam o canal de suporte/resistência exatamente como no indicador MQL.
2. **Perceptrons**
   - **Perceptron 1** mistura a primeira ativação de Laguerre com o primeiro swing de SilverTrend usando pesos `X11 - 100` e `X12 - 100`.
   - **Perceptron 2** mistura a segunda ativação de Laguerre com o segundo swing de SilverTrend usando pesos `X21 - 100` e `X22 - 100`.
   - **Perceptron 3** trabalha nas terceira e quarta ativações de Laguerre com pesos `X31 - 100` e `X32 - 100`.
3. **Supervisor (parâmetro Pass)**
   - `Pass = 3`: requer `Perceptron 3 > 0`. Se também `Perceptron 2 > 0`, a estratégia compra usando `TakeProfit2` / `StopLoss2`. Caso contrário, se `Perceptron 1 < 0`, vende usando `TakeProfit1` / `StopLoss1`.
   - `Pass = 2`: quando `Perceptron 2 > 0`, uma posição comprada é aberta com o segundo conjunto de limites de risco. Se `Perceptron 2 <= 0`, uma vendida é aberta com o primeiro conjunto de limites.
   - `Pass = 1`: quando `Perceptron 1 < 0`, a estratégia vende usando o primeiro conjunto de risco. Caso contrário, vai comprado usando as mesmas configurações de risco.
4. **Gerenciamento de ordens**
   - As entradas são executadas com `BuyMarket` ou `SellMarket` e usam o parâmetro `TradeVolume` como tamanho de lote.
   - Os níveis de take-profit e stop-loss são calculados a partir do preço de fechamento da vela de sinal: `entry ± points * PriceStep`. Eles são monitorados em cada vela terminada através de verificações de máxima/mínima, emulando as ordens de proteção originais do MT5.
   - Novos sinais são ignorados enquanto uma posição está ativa; apenas quando a posição é fechada são avaliados novos trades.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 15 minutos | Tipo de vela usado para cálculos. |
| `TradeVolume` | `decimal` | 0.1 | Volume da posição em lotes. |
| `Risk1`, `Risk2` | `int` | 3 / 9 | Fatores de risco SilverTrend que definem a largura do canal. |
| `Laguerre1Period` – `Laguerre4Period` | `int` | 14 | Comprimento ADX para cada stream de suavização Laguerre. |
| `Laguerre1Distance` – `Laguerre4Distance` | `decimal` | 0 | Distância em porcentagem (0–100) ao redor do limiar 0.5 que define a zona neutra. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | `decimal` | 100 | Coeficientes de peso; o código MQL subtrai 100 antes de aplicá-los. |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | `int` | 100 / 50 | Distâncias de proteção expressas em pontos. |
| `Pass` | `int` | 3 | Modo supervisor que seleciona a combinação de perceptrons usados para trading. |

## Notas de uso
- Os pesos padrão (`100`) neutralizam os perceptrons. Para ativar a estratégia, ajuste os pesos longe de `100` para que os perceptrons possam produzir saídas diferentes de zero.
- A implementação SilverTrend respeita a lógica original de contagem de alertas (sem alertas) e mantém o estado entre velas, portanto os sinais se alinham com a versão MT5.
- Como os níveis de take-profit e stop-loss são simulados internamente, a máxima/mínima de cada vela completada é usada para verificar acertos de alvo. Picos intrabarra entre ticks não são modelados.
- A estratégia é de símbolo único e não gerencia múltiplos instrumentos. Anexe-a ao instrumento desejado e configure a série de velas de acordo.
- Apenas posições compradas ou vendidas são permitidas de cada vez; reverter a posição força uma saída completa primeiro.

## Implantação
1. Compilar a solução e executar a estratégia a partir do lançador de amostras do StockSharp ou incluí-la em um projeto personalizado.
2. Escolher o instrumento, atribuir a série de velas e configurar os pesos do perceptron mais os parâmetros de risco.
3. Iniciar a estratégia e monitorar os trades usando o gráfico criado automaticamente (indicadores Laguerre e negócios próprios são adicionados à área).
4. Otimizações podem ser executadas através dos metadados de parâmetros integrados (`SetCanOptimize`) se desejado.
