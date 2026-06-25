# Exp XWAMI MMRec (ID 2956)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A estratégia replica o assessor especializado do MetaTrader **Exp_XWAMI_MMRec**, combinando o indicador de momentum personalizado XWAMI com um "contador" de gestão de dinheiro. O momentum é medido como a diferença entre o preço atual e o preço `Period` barras atrás. Essa diferença passa por quatro estágios de suavização configuráveis; o terceiro e quarto estágios formam os buffers `Up` e `Down` do indicador original. Cruzamentos entre os dois buffers impulsionam reversões de posição.

Cada estágio pode emular vários algoritmos de suavização: médias móveis simples/exponenciais/suavizadas/ponderadas linearmente, Jurik JJMA/JurX, Tillson T3, VIDYA (aproximado com EMA) e AMA de Kaufman. A estratégia trabalha com uma única posição agregada e suporta operações tanto compradas quanto vendidas. O risco é reduzido após perdas consecutivas comparando os resultados recentes das operações com as janelas `BuyTotalTrigger`/`SellTotalTrigger` e contando perdas relativas a `BuyLossTrigger`/`SellLossTrigger`.

Os stops de proteção seguem a implementação do MetaTrader: `StopLossPoints` e `TakeProfitPoints` são medidos em pontos do símbolo (`Security.PriceStep`). Quando um stop ou alvo é tocado dentro do período de sinal, a posição é fechada imediatamente e o resultado da operação entra no histórico de gestão de dinheiro.

## Parâmetros

| Propriedade StockSharp | Padrão | Entrada original | Descrição |
| --- | --- | --- | --- |
| `CandleType` | Período H1 | `InpInd_Timeframe` | Período usado para construir velas para o indicador. |
| `Period` | 1 | `iPeriod` | Distância (em barras) entre o preço atual e o preço de comparação no cálculo do momentum. |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | Método de suavização, comprimento e fase para o estágio 1. A fase só é usada por Jurik/JurX/T3. |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | Configurações para o segundo estágio de suavização. |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | Configurações para o terceiro estágio (buffer `Up` do indicador). |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | Configurações para o quarto estágio (buffer `Down` do indicador). |
| `AppliedPrice` | `Close` | `IPC` | Fonte de preço encaminhada ao cálculo do momentum. Todas as opções de preço do MetaTrader são reproduzidas, incluindo ambos os sabores TrendFollow e o preço Demark. |
| `SignalBar` | 1 | `SignalBar` | Índice da vela histórica usada para avaliar cruzamentos (`0` = barra terminada mais recente). |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | Habilita entradas compradas ou vendidas respectivamente. |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | Habilita saídas forçadas quando o sinal oposto aparece. |
| `NormalVolume` | `0.1` | `MM` | Tamanho de lote/volume padrão usado após séries lucrativas ou neutras. |
| `ReducedVolume` | `0.01` | `SmallMM_` | Lote reduzido aplicado após muitas perdas. |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | Número de operações compradas recentes inspecionadas e máximo de perdas dentro dessa janela antes de reduzir o volume comprado. |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | Mesma lógica para posições vendidas. |
| `StopLossPoints` | `1000` | `StopLoss_` | Distância do stop-loss em pontos. |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | Distância do take-profit em pontos. |

## Comportamento

1. Assinar a série de velas solicitada e avaliar apenas velas terminadas.
2. Calcular a diferença de preço (`AppliedPrice` agora vs. `Period` barras atrás). Quando houver histórico suficiente, passar a diferença pelos quatro estágios de suavização.
3. Armazenar as saídas do terceiro (`Up`) e quarto (`Down`) estágio. Quando `Up` e `Down` em `SignalBar + 1` (a barra anterior) cruzam, a estratégia muda o viés. Se `Up > Down`, posições vendidas são fechadas e uma posição comprada é aberta se `Up <= Down` na barra de sinal. A lógica oposta trata sinais baixistas.
4. O tamanho da posição é selecionado pelo contador: os últimos `BuyTotalTrigger` (ou `SellTotalTrigger`) lucros de operações são inspecionados. Se pelo menos `BuyLossTrigger` (ou `SellLossTrigger`) deles forem negativos, a próxima operação usa `ReducedVolume`; caso contrário, `NormalVolume` é usado.
5. Quando uma posição comprada existe, as distâncias de stop-loss e take-profit são convertidas de pontos para preço multiplicando por `Security.PriceStep`. Ao violar, a posição é fechada no preço de stop/alvo e a operação é registrada para o módulo de gestão de dinheiro. Operações vendidas seguem as regras simétricas.

## Diferenças da versão MetaTrader

- O StockSharp agrega posições, portanto `BuyMagic`/`SellMagic`, a contabilidade de variáveis globais do MetaTrader e a opção `MarginMode` são desnecessárias e foram omitidas.
- Tillson T3 é implementado explicitamente; Jurik JJMA e JurX mapeiam para `JurikMovingAverage` com a fase fornecida. VIDYA e ParMA são aproximados com uma média móvel exponencial porque o StockSharp não tem equivalentes nativos.
- As ordens são executadas com `BuyMarket`/`SellMarket` e os stops/alvos são aplicados monitorando máximas/mínimas de velas em vez de ordens stop nativas do MT5.
- A entrada de desvio/deslizamento não é necessária nos modelos de execução do StockSharp e foi removida.

## Notas de uso

1. Escolha o instrumento e defina `CandleType` para o período usado pelo especialista original.
2. Configure os métodos e comprimentos de suavização para corresponder às configurações do indicador MetaTrader.
3. Ajuste `NormalVolume`, `ReducedVolume` e os limites de ativação para se alinhar com a política de risco desejada.
4. Anexe a estratégia a uma carteira e inicie-a; o trading é totalmente automatizado e se reverte a cada cruzamento do indicador.

Para maior personalização você pode editar os mapeamentos de suavização dentro de `ExpXwamiMmRecStrategy.CreateFilter` para conectar indicadores alternativos do StockSharp.
