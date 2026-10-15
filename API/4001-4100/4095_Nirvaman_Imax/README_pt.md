# Estratégia Nirvaman Imax
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Nirvaman Imax é uma conversão direta dos MetaTrader 4 consultores especialistas `NirvamanImax.mq4` agrupados com os indicadores personalizados HA, Moving Averages2 e iMAX3alert. A implementação StockSharp mantém a ideia original de combinar velas Heikin-Ashi com um detector de tendência bifásico e um filtro de linha de base EMA enquanto adota o API de alto nível. A estratégia funciona em um único instrumento e período de tempo e fecha automaticamente as negociações após um período de manutenção configurável.

## Indicadores e filtros
- **Heikin-Ashi velas** – reproduz o indicador HA original e classifica as velas como de alta ou de baixa comparando os valores de abertura e fechamento do Heikin.
- **Crossover EMA rápido/lento** – substitui a saída de fase dupla MT4 `iMAX3alert1`. Um sinal de alta aparece quando o EMA rápida cruza acima do EMA lenta; um sinal de baixa ocorre no cruzamento oposto.
- **EMA filtro de tendência** – espelha o buffer `Moving Averages2` EMA e atua como uma linha de base. Somente negociações longas acima do filtro e negociações curtas abaixo dele são permitidas.
- **Filtro de tempo** – ignora qualquer vela cuja hora esteja dentro da janela proibida definida por `NoTradeStartHour` e `NoTradeEndHour` (a janela suporta meia-noite e uma mudança de fuso horário do corretor).
- **Saída temporizada** – cada posição é fechada à força após `CloseAfter` decorrido, reproduzindo a lógica `tiempoCierre` da versão MQL.
- **Stops e metas** – o stop loss e o take-profit são aplicados em etapas de preço derivadas do tamanho do tick do instrumento. Definir como `0` desativa a proteção correspondente.

## Regras de negociação
1. Aguarde até que Heikin-Ashi, EMA rápida, EMA lenta e filtro EMA sejam formados e um fechamento de vela anterior esteja disponível.
2. Rejeite o sinal se o tempo da vela estiver dentro da janela de negociação restrita.
3. Entrada longa:
   - O EMA rápido cruza acima do EMA lento na vela atual.
   - O fechamento de Heikin-Ashi está acima de sua abertura (corpo de alta).
   - O fechamento da vela anterior está acima do filtro EMA.
4. Entrada curta:
   - O EMA rápido cruza abaixo do EMA lento na vela atual.
   - O fechamento de Heikin-Ashi está abaixo de sua abertura (corpo de baixa).
   - O fechamento da vela anterior está abaixo do filtro EMA.
5. Regras de saída:
   - Os níveis de stop loss ou takeprofit são tocados pela faixa de velas.
   - O tempo de vida máximo da posição `CloseAfter` foi excedido.
   - A proteção manual acionada via `StartProtection()` fecha a posição quando o mecanismo a solicita.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Volume base de ordens de mercado. | `0.1` |
| `CandleType` | Período de vela usado para cada indicador e sinal. | `30m` período de tempo |
| `FastTrendLength` | Comprimento do EMA rápida que emula a fase azul do iMAX. | `10` |
| `SlowTrendLength` | Comprimento do EMA lenta que emula a fase vermelha do iMAX. | `21` |
| `FilterLength` | Período EMA para o filtro de linha de base (equivalente a Médias Móveis2). | `13` |
| `StopLoss` | Distância de parada protetora nas etapas de preço; `0` desativa a parada. | `50` |
| `TakeProfit` | Distância alvo de lucro em etapas de preço; `0` desativa o alvo. | `100` |
| `CloseAfter` | Tempo máximo de retenção antes que a posição seja fechada à força. | `15000 s` |
| `NoTradeStartHour` | Hora (0–23) que marca o início da janela de não negociação. | `22` |
| `NoTradeEndHour` | Hora (0–23) que marca o fim da janela de não negociação. | `2` |
| `BrokerTimeOffset` | Deslocamento do fuso horário do corretor (horas) aplicado antes do filtro de horário. | `0` |

## Notas de conversão
- O indicador MT4 `iMAX3alert1` expõe dois buffers codificados por cores. Seu cruzamento é traduzido em um cruzamento rápido/EMA lenta, que preserva a lógica de entrada original orientada a eventos.
- O indicador Moving Averages2 foi executado no modo EMA com um comprimento padrão de 13. A versão StockSharp reutiliza um `ExponentialMovingAverage` padrão com o mesmo padrão.
- O gerenciamento do ciclo de vida da posição reflete o script MQL: a posição é fechada no tempo limite antes que novas entradas possam ser avaliadas e nenhuma lógica adicional de trailing stop foi adicionada.

## Dicas de uso
1. Anexe a estratégia a uma placa/segurança e defina o `CandleType` desejado antes de iniciá-la.
2. Ajuste `TradeVolume`, `StopLoss`, `TakeProfit` e `CloseAfter` para corresponder à volatilidade do instrumento e à tolerância ao risco.
3. Otimize os períodos EMA se precisar aproximar o comportamento do ajuste original do iMAX para um novo mercado.
4. Combine com controles de risco de nível superior (proteção de portfólio, controle de sessão) ao executar múltiplas instâncias.
