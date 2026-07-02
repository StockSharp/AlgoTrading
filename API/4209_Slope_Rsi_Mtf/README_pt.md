# Inclinação RSI Estratégia MTF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia MTF Slope RSI** transporta o MetaTrader 4 consultor especialista `SLOPE_RSI_MTF_LBranjord.mq4` junto com seu indicador complementar `Slope_Direction_Line_Alert.mq4`. A configuração original empilhou várias médias móveis de Hull (denominadas "Linha de direção de inclinação") em vários períodos de tempo e só abriu negociações quando todas elas apontavam na mesma direção, enquanto um filtro RSI de quatro camadas confirmava o impulso. A versão StockSharp reproduz essa lógica de confirmação de vários períodos com assinaturas de alto nível, mantém os alvos de saída baseados em ATR e adiciona amplo suporte de configuração por meio de parâmetros de estratégia.

## Lógica de negociação
1. Assine quatro séries de velas para o mesmo instrumento: o período de negociação (`BaseTimeframe`), uma série de confirmação horária, uma série de quatro horas e uma série diária.
2. Alimente cada série em sua própria `HullMovingAverage` (a substituição StockSharp da linha de direção de inclinação) e `RelativeStrengthIndex` instância. A série base usa `SlopeTriggerLength` (padrão 60) enquanto a série de confirmação usa `SlopeTrendLength` (padrão 200).
3. Acompanhe os dois últimos valores de Hull por período. Um período de tempo é considerado otimista quando o valor atual do Hull está estritamente acima do anterior; é pessimista quando o valor de Hull está estritamente abaixo do valor anterior.
4. Monitore simultaneamente o RSI em cada período:
   - Configuração longa: RSI deve estar acima de `RsiMiddleLevel` (50 por padrão), mas abaixo de `RsiUpperBound` (90) em todas as quatro séries.
   - Configuração curta: RSI deve estar abaixo de `RsiMiddleLevel`, mas acima de `RsiLowerBound` (10) em todas as quatro séries.
5. Quando o período base fechar e todas as confirmações forem de alta, acione um sinal longo. Se todas as confirmações forem de baixa, acione um sinal curto. Os sinais são ignorados até que cada indicador produza pelo menos um valor histórico.
6. Antes de adicionar uma nova posição, calcule distâncias de proteção a partir de valores ATR:
   - A série horária fornece a distância stop-loss.
   - A série diária fornece a distância do take-profit.
7. As entradas no mercado adicionam exposição na direção do sinal, respeitando `MaxOrders`. No ambiente de compensação, a exposição oposta é reduzida antes de uma nova negociação ser adicionada.
8. Os níveis de proteção são recalculados em cada aumento de escala e avaliados nas velas do período base subsequente. Se a máxima/mínima da vela cruzar o nível de stop-loss ou take-profit armazenado, a estratégia sai da posição completa com uma ordem de mercado.

## Gestão de riscos e dimensionamento de posições
- `UseCompounding` ativa a regra de composição do especialista MQL: `volume = PortfolioValue / BalanceDivider`. Quando desativado, `BaseVolume` é usado.
- O auxiliar `AdjustVolume` arredonda o volume solicitado para o `VolumeStep` da segurança e impõe `MinVolume`/`MaxVolume`. O valor ajustado também é gravado em `Strategy.Volume` para que as ações manuais sigam o mesmo tamanho.
- O período ATR (`AtrPeriod`, padrão 21) reflete as configurações originais para cálculos de stop-loss e take-profit. A parada usa o ATR horário, enquanto a meta de lucro usa o ATR diário.
- Os contadores de posição (`_longEntries`, `_shortEntries`) garantem que não mais do que `MaxOrders` aumentos de escala estejam ativos em qualquer direção por vez.

## Manipulação de dados em vários períodos
- Todas as assinaturas são criadas com `SubscribeCandles(...)` e processadas por meio de `Bind`. A estratégia não armazena em cache as velas históricas manualmente; os indicadores reagem aos dados de streaming e expõem seus valores finais por meio dos retornos de chamada `Bind`.
- O auxiliar `TimeframeState` armazena valores de Hull e RSI juntamente com a leitura anterior de Hull, permitindo comparações de inclinação sem solicitar buffers de indicadores históricos.
- Os valores ATR são obtidos somente quando o indicador correspondente reporta `IsFormed`, garantindo que as paradas e metas sejam calculadas a partir de barras completas.

## Parâmetros
| Nome | Tipo | Padrão | MetaTrader contraparte | Descrição |
| --- | --- | --- | --- | --- |
| `SlopeTriggerLength` | `int` | `60` | `SDL1_trigger` | Comprimento do casco no período de negociação. |
| `SlopeTrendLength` | `int` | `200` | `SDL1_period` | Comprimento do casco em confirmações horárias, de quatro horas e diárias. |
| `RsiPeriod` | `int` | `14` | RSI período | RSI lookback aplicado a cada período. |
| `RsiLowerBound` | `decimal` | `10` | RSI limite inferior | Filtro RSI inferior para sinais curtos. |
| `RsiMiddleLevel` | `decimal` | `50` | RSI nível médio (implícito) | Nível RSI neutro que separa regimes longos e curtos. |
| `RsiUpperBound` | `decimal` | `90` | RSI limite superior | Filtro superior RSI para sinais longos. |
| `AtrPeriod` | `int` | `21` | `ATR_Period` | Comprimento ATR para cálculos de stop e take-profit. |
| `MaxOrders` | `int` | `5` | `MaxOrders` | Número máximo de entradas de redução por direção. |
| `UseCompounding` | `bool` | `true` | `compounding` | Permite dimensionamento de posição baseado em portfólio. |
| `BaseVolume` | `decimal` | `0.1` | `Lots` | Lote fixo quando a composição está desativada. |
| `BalanceDivider` | `decimal` | `100000` | implícito (`AccountBalance()/100000`) | Divisor para a fórmula de composição. |
| `BaseTimeframe` | `DataType` | `5m` | prazo do gráfico | Série de velas que impulsiona a execução da negociação. |
| `HourTimeframe` | `DataType` | `1h` | `PERIOD_H1` | Primeira série de confirmação. |
| `FourHourTimeframe` | `DataType` | `4h` | `PERIOD_H4` | Segunda série de confirmação. |
| `DayTimeframe` | `DataType` | `1d` | `PERIOD_D1` | Série de confirmação mais alta. |

## Diferenças do consultor especialista original
- StockSharp opera em modo de compensação, portanto, posições opostas são fechadas antes que uma nova negociação seja aberta. MetaTrader 4 permitiu a cobertura de vários tickets em ambas as direções.
- Paradas e metas de proteção são executadas por meio de monitoramento baseado em velas, em vez de modificações nas ordens do corretor. Isso mantém a lógica dentro da estratégia enquanto reproduz as ATR distâncias do EA original.
- Os valores dos indicadores são fornecidos pelos `HullMovingAverage`, `RelativeStrengthIndex` e `AverageTrueRange` integrados do StockSharp. Nenhum buffer de indicador personalizado é acessado diretamente, em conformidade com as práticas recomendadas de alto nível API.
- Metadados de parâmetros, nomes fáceis de localização e dicas de intervalo são expostos por meio de `Param(...).SetDisplay(...)`, tornando a estratégia mais fácil de configurar e otimizar.

## Notas de uso
- Mantenha os prazos de confirmação estritamente maiores ou iguais ao prazo de negociação. A mistura de períodos mais curtos pode produzir sinais conflitantes e anular o propósito da confirmação da inclinação de vários períodos de tempo.
- Certifique-se de que os metadados de segurança (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) sejam preenchidos para que o arredondamento de parada/alvo e os ajustes de volume se comportem corretamente.
- Como o monitoramento de stop-loss e take-profit ocorre uma vez por vela base concluída, as saídas intrabarras ocorrerão no próximo fechamento da barra. Se for necessária uma gestão intrabar mais rígida, reduza o prazo de negociação ou estenda a estratégia com monitoramento em nível de tick.
- O teste de inclinação do casco exige que valores consecutivos sejam diferentes. As sequências Flat Hull (valores iguais) bloqueiam novas negociações mesmo se os filtros RSI passarem, espelhando a condição "SDL > SDL[1]" do script MetaTrader.
