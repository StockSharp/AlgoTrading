# Estratégia de Four Hour Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Four Hour Swing** porta o assessor especialista "4H swing" do MetaTrader para a API de alto nível do StockSharp. O sistema original combina seguidor de tendência e confirmações de oscilador obtidas de períodos de tempo superiores. Esta versão em C# subscreve três períodos de tempo (entrada, confirmação e filtro macro) e recria a pilha de indicadores com componentes do StockSharp.

## Lógica de negociação
- O filtro de tendência principal usa três médias móveis exponenciais calculadas no preço típico das velas de entrada. Uma configuração comprada requer `Fast EMA > Medium EMA > Slow EMA`; uma configuração vendida espelha a condição.
- Os valores do oscilador Stochastic são avaliados no período de tempo de confirmação superior. A linha %K deve permanecer acima de %D para comprados e abaixo para vendidos.
- O Momentum é amostrado das mesmas velas de confirmação e convertido para a proporção no estilo MetaTrader em torno de 100. Uma operação é permitida apenas se pelo menos uma das últimas três leituras de momentum estiver mais longe do que o limiar configurado.
- Os valores mensais de MACD (ou definidos pelo usuário) fornecem o filtro de direção macro. Uma compra requer a linha MACD acima do seu sinal, enquanto uma venda verifica a relação oposta.

Uma posição é aberta na próxima vela base assim que todas as confirmações estiverem alinhadas e a conta estiver plana ou posicionada na direção oposta (nesse caso a ordem de mercado fecha e reverte).

## Gestão de risco
- Distâncias fixas de stop-loss e take-profit (expressas em pips) são aplicadas imediatamente após a entrada.
- Um Trailing stop opcional segue o preço extremo alcançado após a entrada.
- A proteção de break-even pode mover o stop para o preço de entrada mais um deslocamento quando a distância de ativação configurada for atingida.
- Uma saída MACD opcional fecha operações abertas quando o filtro macro vira.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `TradeVolume` | Volume de ordem de mercado padrão. | `0.01` |
| `CandleType` | Tipo de vela de entrada (ex.: velas de 4 horas). | `4H` |
| `SignalCandleType` | Tipo de vela de confirmação para Stochastic e Momentum. | `7D` (semanal) |
| `MacdCandleType` | Tipo de vela de filtro macro. | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | Comprimentos de EMA calculados no preço típico. | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | Configurações do oscilador Stochastic. | `13`, `5`, `5` |
| `MomentumPeriod` | Período de retrovisão usado pelo indicador de Momentum. | `14` |
| `MomentumThreshold` | Distância mínima de 100 necessária para validar o Momentum. | `0.3` |
| `StopLossPips`, `TakeProfitPips` | Ordens de proteção em pips. | `20`, `50` |
| `TrailingStopPips` | Distância do Trailing stop em pips. Defina como zero para desativar. | `40` |
| `UseBreakEven` | Ativa a proteção de break-even. | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Ativador e deslocamento para o movimento de break-even. | `30`, `30` |
| `UseMacdExit` | Fechar posições quando o MACD macro vira. | `false` |

## Notas
- Os recursos de gerenciamento de capital (stops de patrimônio, metas em moeda) do especialista original são intencionalmente omitidos para manter a implementação compacta.
- A estratégia processa apenas velas concluídas, correspondendo à avaliação barra a barra do MetaTrader.
- Os períodos de tempo padrão reproduzem a configuração comum de 4 horas (confirmação semanal e filtro mensal), mas cada parâmetro `DataType` pode ser reconfigurado para executar em períodos alternativos.
