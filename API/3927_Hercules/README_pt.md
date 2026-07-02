# Estratégia Hércules
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Hercules é uma versão StockSharp do especialista MetaTrader **Hercules v1.3 (Majors)**. Ele combina um cruzamento de média móvel rápida/lenta com filtros de confirmação de vários períodos de tempo e executa duas metas de lucro independentes por sinal.

## Lógica de negociação

* **Braço de sinal** – calcula um EMA rápido (padrão 1 período) no fechamento da vela e um SMA lento (72 períodos) na abertura da vela. Detecte cruzamentos que aconteceram na última ou penúltima barra. O preço cruzado é calculado em média entre ambas as médias móveis e um nível de gatilho é colocado `TriggerPips` acima (para posições compradas) ou abaixo (para posições vendidas).
* **Janela de execução** – uma vez detectado um cruzamento, a configuração permanece válida por duas barras completas. Somente quando o fechamento atual exceder o preço de gatilho dentro desta janela a ordem poderá ser disparada.
* **Filtros** –
  * H1 RSI (comprimento padrão 10, entrada de preço típica) deve estar acima de `RsiUpper` para posições compradas e abaixo de `RsiLower` para posições vendidas.
  * O fechamento atual deve quebrar a máxima/mínima recente coletada em `LookbackMinutes` velas no período de negociação.
  * O envelope diário (SMA 24 com ±`DailyEnvelopeDeviation`%) exige que o preço feche fora da banda na direção da negociação.
  * O envelope H4 (SMA 96 com ±`H4EnvelopeDeviation`%) adiciona uma segunda confirmação de período de tempo superior.
* **Gerenciamento de risco** – o stop-loss é definido para o máximo/mínimo da barra quatro velas atrás. O volume pode ser fixo (`OrderVolume`) ou recalculado a partir de `RiskPercent` do valor atual do portfólio.
* **Gestão comercial** – cada sinal abre duas ordens de mercado de igual volume. O primeiro é liquidado em `TakeProfitFirstPips`, o segundo em `TakeProfitSecondPips`. Um trailing stop de `TrailingStopPips` mantém ambas as ordens protegidas. Quando o stop ou ambos os alvos são concluídos, a estratégia entra em um período de blackout de `BlackoutHours` durante o qual nenhuma nova negociação é realizada.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `OrderVolume` | Volume de cada ordem de mercado antes dos ajustes de gestão monetária. |
| `UseMoneyManagement` | Quando ativado, recalcula o volume de `RiskPercent` do portfólio e a distância de parada atual. |
| `RiskPercent` | Porcentagem do valor do portfólio em relação ao risco por configuração. |
| `TriggerPips` | Distância do preço de cruzamento que deve ser ultrapassada para permitir uma entrada. |
| `TrailingStopPips` | Distância do trailing stop em pips aplicada à posição combinada. |
| `TakeProfitFirstPips` | Distância pip do primeiro take-profit parcial. |
| `TakeProfitSecondPips` | Distância pip do segundo take-profit parcial. |
| `FastPeriod` | Comprimento da linha de disparo rápida EMA. |
| `SlowPeriod` | Comprimento da linha de base lenta SMA. |
| `RsiPeriod` | Comprimento do filtro de confirmação RSI. |
| `RsiUpper` / `RsiLower` | RSI limites que permitem negociações longas e curtas. |
| `LookbackMinutes` | Janela (em minutos) usada para calcular o filtro de rompimento de máxima/mínima recente. |
| `BlackoutHours` | Horas para pausar após uma execução antes de aceitar uma nova configuração. |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | Parâmetros do filtro de envelope diário. |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | Parâmetros do filtro envelope H4. |
| `CandleType` | Prazo principal utilizado para execução da negociação. |
| `RsiTimeFrame` | Período que alimenta o indicador RSI. |
| `DailyTimeFrame` | Prazo que alimenta o cálculo do envelope diário. |
| `H4TimeFrame` | Prazo que alimenta o cálculo do envelope H4. |

## Arquivos

* `CS/HerculesStrategy.cs` – implementação em C# da estratégia Hercules.
* `README.md` – este documento.
* `README_ru.md` – Descrição russa.
* `README_zh.md` – descrição chinesa.
