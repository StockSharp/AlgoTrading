# Sistema Muito Blondie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O Sistema Very Blondie é uma estratégia de grade de reversão à média de curto prazo originalmente distribuída como o MetaTrader 4 consultor especialista "VBS - Sistema Very Blondie". A porta mantém a ideia original de desvanecer um rompimento da faixa de negociação recente: quando o preço se afasta o suficiente da máxima mais alta ou da mínima mais baixa observada nas últimas `PeriodX` velas, a estratégia entra imediatamente com uma ordem de mercado e adiciona quatro ordens de limite no estilo martingale para escalar o movimento se o preço continuar se estendendo.

## Dados e Indicadores
- **Dados primários**: uma única série de velas configurada pelo parâmetro `CandleType` (a versão MQL é negociada no período do gráfico).
- **Indicadores**: indicadores `Highest` e `Lowest` (comprimento = `PeriodLength`) rastreiam os extremos da faixa rolante usados para detecção de fugas.
- **Cotações de nível 1**: os melhores preços de compra/venda são consumidos para colocar ordens de mercado e limitar as compensações MT4 originais.

## Lógica de entrada
1. Em cada vela finalizada, calcule a máxima mais alta e a mínima mais baixa nas últimas `PeriodLength` barras.
2. Leia o melhor lance/pedido atual (substituição para o fechamento da vela se faltarem cotações).
3. **Configuração longa**: se `highest - bid > LimitPoints * PointValue`, envie uma ordem de compra a mercado com o volume base e coloque quatro ordens de compra com limite abaixo da venda. Cada ordem limite fica `GridPoints * PointValue` mais distante e dobra o volume da ordem anterior (1×, 2×, 4×, 8×, 16×).
4. **Configuração curta**: se `bid - lowest > LimitPoints * PointValue`, envie uma ordem de venda a mercado e quatro ordens de venda com limite acima do lance nas mesmas distâncias e multiplicadores de volume da lógica de compra.
5. Apenas uma cesta pode estar ativa por vez. Novos sinais são ignorados até que todas as posições e ordens pendentes do ciclo anterior desapareçam.

## Gerenciamento de posição
- **Meta de lucro flutuante**: o parâmetro `Amount` original monitorado `OrderProfit + OrderSwap` em todas as negociações. A porta reproduz isso com a posição agregada: `(close - entryPrice) * position * conversionFactor >= ProfitTarget`. Quando o limite é atingido, todas as posições são fechadas com ordens de mercado e todas as ordens de grade restantes são canceladas.
- **Bloqueio de equilíbrio**: quando `LockDownPoints > 0`, o código MT4 moveu o stop loss de cada ordem preenchida para `entry price ± Point` quando a negociação obteve `LockDownPoints` pontos de lucro. A versão StockSharp rastreia a posição líquida; assim que o preço avança em `LockDownPoints * PointValue` o nível de equilíbrio é armado em `entryPrice ± PointValue`. Se uma vela posterior atingir esse nível (mínimo para posições compradas, alta para posições vendidas), toda a cesta será achatada e todas as ordens pendentes serão canceladas.
- **Saídas manuais**: interromper a estratégia ou atingir as condições de lucro/ponto de equilíbrio sempre cancela as quatro ordens de limite pendentes para imitar a rotina `CloseAll()` do MT4.

## Gestão de capital
- **Volume base**: corresponde à expressão MT4 `MathRound(AccountBalance()/100) / 1000`. A estratégia lê o valor atual da carteira (ou valor inicial quando nenhuma negociação foi feita), arredonda-o de zero e converte-o em lotes. O resultado é alinhado a `Security.VolumeStep`, obedece a `MinVolume`/`MaxVolume` e recorre à estratégia `Volume` (ou `1`) quando o instantâneo do portfólio não está disponível.
- **Martingale grade**: cada ordem de limite adicional dobra o volume base em até quatro níveis (1×, 2×, 4×, 8×, 16×). Os volumes são normalizados com o mesmo ajudante para evitar o envio de lotes fracionários rejeitados pelo local.
- **Parâmetro PointValue**: `Point` do MT4 pode ser diferente de `Security.PriceStep` (especialmente em cotações FX de 5 dígitos). O padrão de `PointValue` é a detecção automática de `PriceStep`/`Step`, mas você pode substituí-la para corresponder precisamente ao comportamento do EA original.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `PeriodLength` | Janela de lookback para o máximo mais alto e o mínimo mais baixo | `60` |
| `LimitPoints` | Distância mínima (em pontos MT4) entre o preço atual e o extremo do intervalo para acionar uma cesta | `1000` |
| `GridPoints` | Espaçamento (em pontos MT4) entre ordens de grade consecutivas | `1500` |
| `ProfitTarget` | Meta de lucro flutuante expressa na moeda da conta | `40` |
| `LockDownPoints` | Distância de lucro (em pontos MT4) que arma a saída do ponto de equilíbrio | `0` |
| `PointValue` | Alteração de preço produzida por um ponto MT4 (`0` = detecção automática) | `0` |
| `CandleType` | Série de velas usada para impulsionar a estratégia | `TimeFrameCandle, 1 minute` |

## Portando Notas
- O PnL flutuante é aproximado com a posição agregada em vez de somar o `OrderProfit + OrderSwap` de cada pedido. Isso corresponde ao comportamento original quando todas as negociações estão na mesma direção, que é como o EA funciona.
- A modificação do stop loss é emulada por uma saída imediata do mercado ao preço de equilíbrio armado; StockSharp mantém a lógica na camada de estratégia em vez de enviar solicitações `OrderModify`.
- As ordens com limite pendentes são registradas com preços normalizados usando `Security.ShrinkPrice`. Quando os metadados de segurança não possuem um `PriceStep`, defina `PointValue` manualmente para evitar grades desalinhadas.
- A estratégia assume um instrumento e usa ajudantes API de alto nível (`SubscribeCandles`, `SubscribeLevel1`, `BuyLimit`, `SellLimit`, etc.) conforme solicitado nas diretrizes de conversão.
