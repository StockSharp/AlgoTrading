# Estratégia de Breakout Duplo MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Breakout Double MA** é uma versão StockSharp do MetaTrader consultor especialista `DoubleMA_Breakout`. A estratégia monitora uma média móvel rápida e lenta nas velas finalizadas. Quando a média rápida se move acima da lenta, uma ordem de compra stop é colocada a uma distância de rompimento configurável acima do último fechamento. Quando a média rápida cai abaixo da lenta, um stop de venda é colocado simetricamente abaixo do mercado. As ordens pendentes são canceladas e as posições abertas são achatadas quando o cruzamento vira ou a janela de negociação fecha.

A conversão mantém a lógica de breakout principal, adiciona gerenciamento de pedidos de alto nível e expõe configuração extensa por meio de parâmetros `StrategyParam<T>`. Todos os comentários no código foram reescritos em inglês conforme solicitado.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `FastMaPeriod` | 2 | Período da média móvel rápida. |
| `SlowMaPeriod` | 5 | Período da média móvel lenta. |
| `FastMaMode` | `Simple` | Tipo de média móvel para a linha rápida (SMA, EMA, SMMA, LWMA, LSMA). |
| `SlowMaMode` | `Simple` | Tipo de média móvel para a linha lenta. |
| `FastAppliedPrice` | `Close` | Preço aplicado para a média rápida (fechamento, abertura, máximo, mínimo, mediana, típica, ponderada). |
| `SlowAppliedPrice` | `Close` | Preço aplicado para a média lenta. |
| `SignalShift` | 1 | Número de velas concluídas para analisar ao avaliar o cruzamento. `0` significa a vela atual. |
| `BreakoutDistancePoints` | 45 | Distância de rompimento nas etapas de preço usadas para colocar ordens de stop longe do último fechamento. |
| `UseTimeWindow` | `true` | Ativa o filtro de hora de início/parada. |
| `StartHour` | 11 | Primeira hora (inclusive) em que novas negociações são permitidas. |
| `StopHour` | 16 | Última hora (inclusive) em que a negociação é permitida. |
| `UseFridayCloseAll` | `true` | Feche as posições e cancele todas as ordens pendentes quando chegar o horário de fechamento de sexta-feira. |
| `FridayCloseTime` | 21h30 | Hora do dia de sexta-feira em que a estratégia apresenta um hard flat. |
| `UseFridayStopTrading` | `false` | Desative novas entradas após o horário de parada de sexta-feira configurado, mantendo as posições existentes. |
| `FridayStopTradingTime` | 19:00 | Hora do dia de sexta-feira em que novas entradas são bloqueadas (se habilitadas). |
| `CandleType` | 1 hora | Tipo de dados Candle usado para indicadores e sinais. |

## Lógica de negociação
1. Assine as velas finalizadas definidas por `CandleType` e calcule duas médias móveis de acordo com os modos selecionados e preços aplicados.
2. Mantenha históricos curtos de valores de indicadores para que a estratégia possa fazer referência à vela selecionada por `SignalShift` sem violar a diretriz "no GetValue".
3. **Configuração de alta:** quando a MM rápida estiver acima da MM lenta na vela de sinalização, cancele qualquer stop de venda, feche as posições vendidas e coloque uma ordem de stop de compra `BreakoutDistancePoints × PriceStep` acima do último fechamento se não houver ordens ou posições restantes.
4. **Configuração de baixa:** quando a MM rápida estiver abaixo da MM lenta na vela sinalizadora, cancele qualquer stop de compra, feche posições longas e coloque uma ordem de stop de venda na mesma distância abaixo do mercado.
5. **Gerenciamento de tempo:** se a janela de negociação estiver desativada ou fechada, todas as ordens pendentes serão canceladas. Às sextas-feiras, os horários opcionais de stop-trading e hard-flat são respeitados antes do fim de semana.
6. Quando uma ordem stop é executada, a ordem pendente oposta é cancelada para evitar múltiplas negociações simultâneas.

## Diferenças do MetaTrader EA
- Os switches de gerenciamento de dinheiro e os esquemas de trailing stop personalizados do script original não são portados. A propriedade `Volume` de StockSharp define o tamanho da negociação e o controle de risco pode ser adicionado por meio de módulos de proteção padrão.
- Novas tentativas de erro e loops de pedidos de baixo nível são substituídos por auxiliares StockSharp de alto nível (`BuyStop`, `SellStop`, `ClosePosition`, `CancelOrder`).
- Conceitos específicos da corretora, como cortes de margem ou correções de derrapagem, são omitidos; estes podem ser implementados separadamente, se necessário.
- O modo LSMA usa o indicador `LinearRegression` de StockSharp para aproximar a média móvel de mínimos quadrados usada em MetaTrader.

## Notas de uso
- Configure `Volume` antes de iniciar a estratégia; por padrão, StockSharp usa um único lote/contrato.
- Combine a estratégia com `StartProtection` (já invocado no código) para anexar módulos de stop-loss ou take-profit no nível da plataforma, se necessário.
- Para fluxos de trabalho de otimização, habilite os parâmetros desejados por meio das configurações `.SetCanOptimize` fornecidas no construtor.
- Certifique-se de que o instrumento tenha um `PriceStep` válido; caso contrário, a distância de fuga volta para `1` para evitar deslocamentos de zero.
