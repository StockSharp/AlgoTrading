# MACD Exemplo de estratégia 1010
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Este módulo transporta o consultor especialista MetaTrader **macd_sample_1010.mq4** para o StockSharp API de alto nível. O script original combinava bandas Bollinger com regras simples de gerenciamento de dinheiro: quando o preço de fechamento terminava acima da banda superior mais um buffer configurável, ele abria uma ordem de venda, enquanto um fechamento abaixo da banda inferior menos o buffer acionava uma ordem de compra. As posições foram encerradas assim que um valor fixo de lucro ou perda (expresso em pips) foi alcançado. A versão StockSharp reproduz a mesma lógica assinando a série de velas solicitada, vinculando um indicador `BollingerBands` e emitindo ordens de mercado e chamadas de gerenciamento de posição a partir do retorno de chamada da vela.

A conversão mantém o comportamento do especialista legado nas velas finalizadas. Cada avaliação acontece quando uma vela está totalmente formada, garantindo que as decisões de rompimento e saída correspondam ao processamento de fechamento da barra da plataforma MetaTrader. O escalonamento opcional do volume de negociação baseado em saldo também é implementado para emular o sinalizador `LotIncrease` do código MQL4.

## Notas de conversão
- Usa o fluxo de trabalho de alto nível `SubscribeCandles` + `Bind` para alimentar o indicador `BollingerBands` sem buffers personalizados.
- Emprega a infraestrutura StockSharp `StrategyParam<T>` para que todas as entradas fiquem visíveis na interface do usuário e prontas para otimização.
- Chama `BuyMarket` e `SellMarket` com deslocamentos calculados que respeitam o `PriceStep` do instrumento, correspondendo aos cálculos baseados em pip em MetaTrader.
- Implementa o escalonamento de lote opcional por meio de `Portfolio.CurrentValue` (com `BeginValue` como substituto) e limita o volume resultante a 500 lotes, assim como o especialista original.
- Funciona estritamente com velas concluídas para evitar a rotatividade tick-by-tick que o script original controlava por meio de contadores de barras.
- Adiciona comentários descritivos em inglês para esclarecer a intenção de cada bloco de processamento.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `ProfitTargetPips` | `decimal` | `3` | Número de pips de movimento favorável necessários para fechar uma posição com lucro. Defina como `0` para desativar a regra de obtenção de lucro. |
| `LossLimitPips` | `decimal` | `20` | Número de pips de movimento adverso tolerados antes da liquidação da posição. Defina como `0` para desativar a regra de stop-loss. |
| `BandDistancePips` | `decimal` | `3` | Buffer (em pips) adicionado acima da banda superior e abaixo da banda inferior antes que um rompimento seja confirmado. |
| `BollingerPeriod` | `int` | `4` | Número de velas usadas para calcular as bandas Bollinger. |
| `BollingerDeviation` | `decimal` | `2` | Multiplicador de desvio padrão aplicado pelo indicador Bollinger Bandas. |
| `BaseVolume` | `decimal` | `1` | Tamanho inicial da negociação, expresso em lotes. Esse valor também é usado como linha de base para a lógica de escalabilidade. |
| `LotIncrease` | `bool` | `true` | Quando ativado, recalcula o volume de negociação em cada vela para seguir a relação entre o saldo atual da carteira e o saldo inicial. |
| `OneOrderOnly` | `bool` | `true` | Impede que a estratégia abra uma nova posição quando uma já estiver ativa. Quando desativado, a posição líquida ainda é gerenciada porque StockSharp usa posições agregadas. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Série de velas usada para cálculos de indicadores e decisões de negociação. |

## Lógica de negociação
1. `OnStarted` cria o indicador Bollinger Bands com o período e desvio configurados, assina o fluxo de dados `CandleType` e vincula o método `ProcessCandle`.
2. Cada vela finalizada aciona `ProcessCandle`, que recalcula o volume de negociação atual (se `LotIncrease` estiver ativo) antes de avaliar os sinais.
3. Se o preço de fechamento for maior que a banda superior mais `BandDistancePips` (convertido em unidades de preço com `PriceStep`), a estratégia envia uma ordem de venda a mercado. Se o preço de fechamento estiver abaixo da banda inferior menos o buffer, ele envia uma ordem de compra a mercado. Quando `OneOrderOnly` é `true` novas entradas só são tentadas quando a posição líquida é zero.
4. Depois que as entradas potenciais são processadas, a estratégia inspeciona a posição atual:
   - As posições longas são fechadas quando a distância do lucro atinge `ProfitTargetPips` ou quando a perda atinge `LossLimitPips`.
   - As posições curtas são fechadas quando o preço de fechamento se move a favor em `ProfitTargetPips` ou contra em `LossLimitPips`.
5. Todas as comparações de lucros e perdas são realizadas em unidades de preço derivadas do símbolo `PriceStep`, replicando fielmente as verificações baseadas em pip no especialista MetaTrader.

## Lógica de dimensionamento de posição
- Quando `LotIncrease` está desabilitado, a estratégia negocia o valor constante `BaseVolume` em cada sinal.
- Quando `LotIncrease` está habilitado, a primeira vela armazena o saldo inicial por lote (`initial balance / BaseVolume`). As velas subsequentes calculam a proporção entre o saldo atual e essa linha de base, arredondam-na para uma casa decimal (imitando `NormalizeDouble(..., 1)` de MQL4) e limitam o resultado a um máximo de 500 lotes. O valor calculado é então usado como o volume do pedido para a próxima negociação.
- Se as informações do portfólio não estiverem disponíveis, a estratégia volta normalmente para o `BaseVolume` estático.

## Diretrizes de uso
1. Anexe a estratégia ao instrumento desejado e confirme se `Security.PriceStep` reflete o tamanho do pip que você pretende negociar.
2. Selecione o período da vela em `CandleType`. O script original normalmente era executado em intervalos intradiários (5 a 15 minutos), mas qualquer tamanho de barra pode ser usado.
3. Ajuste as configurações de banda, compensações de pip e controles de risco para corresponder às suas preferências de negociação.
4. Decida se o tamanho da posição deve ser escalonado com o saldo da conta (`LotIncrease`) ou permanecer fixo.
5. Comece a estratégia. Monitore o log para verificar se as entradas e saídas ocorrem em velas concluídas nos níveis de preços esperados.

## Diferenças da versão MetaTrader
- StockSharp funciona com posições agregadas, portanto, mesmo quando `OneOrderOnly` está desativado, o resultado é uma única posição líquida em vez de vários tickets independentes.
- As regras de take-profit e stop-loss são implementadas diretamente na estratégia em vez de registrar ordens pendentes com níveis de preços específicos, mas o comportamento resultante é equivalente porque as verificações ocorrem em cada vela finalizada.
- Os sinalizadores de registro (`logging`, `logerrs`, `logtick`) do especialista original são omitidos; O registro integrado do StockSharp já registra pedidos e eventos comerciais.
- A geração de registros e estatísticas baseadas em arquivo da versão MetaTrader não são recriadas porque StockSharp expõe análises mais ricas por meio de portfólios e estratégias.
