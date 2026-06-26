# Estratégia de Operação Contrária MA da Segunda-feira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o assessor especialista MetaTrader **"Contrarian trade MA"** usando a API de alto nível do StockSharp. Combina contexto semanal com um filtro de entrada apenas na segunda-feira para operar contra extremos. O sistema aguarda uma nova semana de trading, mede o quanto a semana anterior fechou em relação ao maior máximo e ao menor mínimo ao longo da janela de lookback, e verifica se o preço abriu a nova semana no lado oposto de uma média móvel deslocada. Se o mercado terminar a primeira vela diária da semana fora desses limites, uma posição contrária é aberta.

A lógica depende apenas de velas completadas. Uma série diária (padrão) impulsiona entradas e saídas, enquanto uma série semanal fornece os níveis extremos e o sinal de média móvel. Cada vez que uma vela de segunda-feira é completada, a estratégia avalia se a semana anterior terminou acima da faixa de máximos recentes ou abaixo da faixa de mínimos recentes, ou se o valor anterior da média móvel está do outro lado do preço de abertura semanal atual. A suposição é que tais movimentos sobreextendidos tendem a reverter à média durante a semana.

## Como funciona

1. Velas semanais alimentam dois indicadores:
   - `Highest`/`Lowest` encontram o máximo e mínimo extremos ao longo de `CalcPeriod` semanas.
   - Uma média móvel configurável (`MaPeriod`, `MaMethod`, `MaShift`, `AppliedPrice`) processa as mesmas velas semanais.
2. Velas diárias (ou qualquer `TradeCandleType` selecionado) acionam decisões de trading assim que se completam.
3. Na primeira vela completada cujo `OpenTime.DayOfWeek == Monday`, a estratégia avalia as condições de entrada:
   - **Comprado** se o fechamento semanal anterior estiver acima do maior máximo do lookback ou se o valor anterior da MA for maior que o preço de abertura semanal atual (significando que o preço abriu abaixo da MA).
   - **Vendido** se o fechamento semanal anterior estiver abaixo do menor mínimo do lookback ou se o valor anterior da MA for menor que o preço de abertura semanal atual (preço abriu acima da MA).
4. As ordens são enviadas com `BuyMarket` ou `SellMarket` usando o volume da estratégia sem médias. Apenas uma posição pode estar aberta de cada vez.

## Gestão de saída

- Uma distância de stop-loss fixa é calculada como `StopLossPips * Security.PriceStep`. Quando habilitado (> 0), a estratégia monitora máximas e mínimas de velas diárias; se o preço tocar o nível de stop dentro do dia, a posição é fechada a mercado.
- Uma saída baseada em tempo fecha qualquer posição aberta após sete dias desde a entrada (`604800` segundos no EA original). A verificação é realizada em cada vela diária completada.
- A estratégia nunca abre uma nova operação até que a anterior esteja completamente fechada.

## Indicadores e dados

- **Extremos semanais:** indicadores `Highest` e `Lowest` anexados à série `MaCandleType` (padrão: velas de 1 semana).
- **Média móvel semanal:** os métodos `Simple`, `Exponential`, `Smoothed` ou `LinearWeighted` estão disponíveis. A média móvel pode ser deslocada para frente por `MaShift` barras para imitar a configuração do MetaTrader e pode consumir diferentes fontes de preço (`AppliedPrice`).
- **Período primário:** `TradeCandleType` define quais velas impulsionam o timing das operações; o padrão são velas diárias para que as entradas sejam avaliadas após o primeiro dia da semana de trading ser fechado.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CalcPeriod` | `int` | `4` | Número de velas de período de tempo superior usadas para calcular o maior máximo e menor mínimo. |
| `StopLossPips` | `int` | `300` | Distância de stop-loss em passos de preço. Defina como `0` para desabilitar o stop de proteção. |
| `MaPeriod` | `int` | `7` | Comprimento da média móvel semanal. |
| `MaShift` | `int` | `0` | Deslocamento para frente da média móvel em barras. Espelha o parâmetro de deslocamento de MA do MetaTrader. |
| `MaMethod` | `MovingAverageMethod` | `LinearWeighted` | Método de cálculo da média móvel (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `AppliedPrice` | `AppliedPriceType` | `Weighted` | Fonte de preço alimentada à média móvel (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `TradeCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Período primário que aciona entradas e gerencia stops/saídas. |
| `MaCandleType` | `DataType` | `TimeSpan.FromDays(7).TimeFrame()` | Período superior usado para a média móvel e para calcular os extremos. |

## Notas

- A distância de stop-loss adapta-se ao instrumento multiplicando a contagem de pips por `Security.PriceStep`. Instrumentos sem um passo definido desativarão efetivamente o stop.
- Como a estratégia avalia velas completadas, as entradas ocorrem no fechamento da barra de segunda-feira e não no primeiro tick da semana. Isso mantém o comportamento determinístico entre os backtests.
- A lógica assume apenas uma posição aberta; qualquer operação aberta é fechada pelo stop-loss ou pelo timeout de sete dias antes que um novo sinal seja considerado.
