# Estratégia de teste Rsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
`RsiTestStrategy` converte o MetaTrader 4 consultor especialista **RSI_Test** em StockSharp de alto nível API. A estratégia combina um filtro de impulso rápido RSI com confirmação simples de velas e dimensionamento de posição consciente do risco. Ele negocia um único instrumento definido pela estratégia hospedeira e usa apenas velas concluídas, refletindo a lógica tick-to-close do código original.

## Regras de negociação
1. Calcule o Índice de Força Relativa com o configurável `RsiPeriod`.
2. Opere comprado quando o RSI estiver subindo de uma região de sobrevenda (`BuyLevel`) *e* a vela atual abrir acima da anterior.
3. Opere vendido quando o RSI estiver caindo de uma região de sobrecompra (`SellLevel`) *e* a vela atual abrir abaixo da anterior.
4. Respeite o limite de `MaxOpenPositions`. Um valor de `0` desativa o limite; caso contrário, a exposição líquida não poderá exceder `MaxOpenPositions * Volume`.
5. Gerencie as saídas por meio de um trailing stop em estilo de escada que é ativado quando o preço avança `TrailingDistanceSteps` ticks além do preço médio de entrada.
6. Nenhum take-profit explícito é usado. As posições saem quando o trailing stop é acionado ou quando a sessão de negociação encerra a estratégia.

## Dimensionamento de posição e risco
* A estratégia deriva um tamanho de pedido provisório de `RiskPercentage` do valor atual do portfólio. Quando o instrumento fornece dados de margem (`Security.MarginBuy`/`Security.MarginSell`) o capital exigido por lote é honrado; caso contrário, o valor será dividido pelo último preço de fechamento como uma alternativa conservadora.
* Os volumes são arredondados para `Security.VolumeStep` (ou duas casas decimais se a etapa for desconhecida) e limitados dentro do intervalo `Security.MinVolume`/`Security.MaxVolume`.
* Defina `RiskPercentage` como zero para desativar o dimensionamento dinâmico e sempre negociar o `Volume` configurado.

## Comportamento de parada móvel
* `TrailingDistanceSteps` expressa a distância em etapas de preço (`Security.PriceStep`). Se o instrumento não expor um passo, a distância é tratada como uma compensação direta de preço.
* Uma vez que o fechamento ou a máxima intrabarra cruza o nível de ativação (`entry + distance` para posições compradas, `entry - distance` para posições vendidas), a estratégia arma o trailing stop no mesmo deslocamento além do preço de entrada.
* O stop de proteção é aplicado apenas uma vez por posição, exatamente como o EA original que move o stop do ponto de equilíbrio para o primeiro degrau e o mantém lá.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `RsiPeriod` | RSI período de retrospectiva. | `14` |
| `BuyLevel` | Limite de sobrevenda que prepara uma configuração longa. | `12` |
| `SellLevel` | Limite de sobrecompra que prepara uma configuração curta. | `88` |
| `RiskPercentage` | Participação do portfólio usada para dimensionamento de posição. Defina `0` para ignorar. | `10` |
| `TrailingDistanceSteps` | Distância (em etapas de preço) necessária para armar o trailing stop. | `50` |
| `MaxOpenPositions` | Máximas posições simultâneas; `0` remove o limite. | `1` |
| `CandleType` | Prazo principal para cálculos. | `15` minutos |
| `Volume` | Volume base quando o dimensionamento do risco não pode ser resolvido. | `1` |

## Notas de uso
1. Anexe a estratégia a um título que exponha `PriceStep`, `VolumeStep` e metadados de margem precisos para a melhor correspondência com o comportamento MQL.
2. O algoritmo verifica apenas velas concluídas (`CandleStates.Finished`), portanto, os backtests devem usar o mesmo período de produção.
3. `StartProtection()` da classe base está habilitado em `OnStarted`, permitindo que o controle de risco integrado de StockSharp gerencie remanescentes de posição inesperados.
4. Como o consultor especialista original lançou MetaTrader otimizações por meio de `GlobalVariableGet`, esse comportamento é omitido intencionalmente. Configure os parâmetros diretamente em StockSharp.
5. Combine a estratégia com um portfólio que atualize `Portfolio.CurrentValue` para dimensionamento dinâmico de risco. Sem isso, a estratégia volta normalmente para a estática `Volume`.
