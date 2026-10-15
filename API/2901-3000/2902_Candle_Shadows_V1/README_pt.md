# Estratégia de Sombras de Candle V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Sombras de Candle V1 é uma estratégia de reversão de ação do preço que recria a lógica original do consultor especialista MetaTrader dentro da API de alto nível do StockSharp. O sistema procura candles com um pavio dominante forte e sombra oposta mínima durante uma sessão de trading configurável. Os trades só são permitidos durante os primeiros minutos de uma barra, emulando a execução intrabar da versão MQL enquanto ainda trabalha em candles fechados.

## Lógica de trading
1. Assinar os candles do período configurado (padrão 5 minutos) e avaliar apenas as barras finalizadas.
2. Aplicar uma janela de sessão usando os parâmetros `StartHour` e `EndHour`. Se o candle abrir fora da janela, nenhum trade é considerado.
3. Permitir entradas somente se o candle fechar antes de `OpenWithinMinutes` desde o horário de abertura, prevenindo sinais tardios em barras longas.
4. Setup comprado: o candle deve imprimir uma sombra inferior maior que `CandleSizeMinPips` pips e a sombra superior deve permanecer dentro de `OppositeShadowMaxPips` pips. Quando as condições são satisfeitas e não há posição aberta, uma compra de mercado é enviada.
5. Setup vendido: o candle deve imprimir uma sombra superior maior que `CandleSizeMinPips` pips e a sombra inferior deve permanecer dentro de `OppositeShadowMaxPips` pips. Uma venda de mercado é emitida se a conta estiver flat.
6. Apenas um trade por candle é permitido, correspondendo à restrição original de "uma ordem por barra".

## Gestão de posição
- As distâncias protetoras iniciais são expressas em pips e convertidas através do parâmetro `PipValue` para cada instrumento.
- Verificações duras de stop-loss e take-profit são realizadas em cada candle concluído. Se o máximo/mínimo do candle tocar o limiar, a posição é zerada.
- O gerenciamento de trailing imita o trailing stop do MQL: uma vez que o preço avança pelo menos `TrailingStopPips + TrailingStepPips`, o stop é movido em incrementos de `TrailingStepPips` pips.
- Se uma posição permanecer aberta por mais de `PositionLivesBars` barras, ela é fechada imediatamente. Trades lucrativos também são forçados a sair após `CloseProfitsOnBar` barras para garantir ganhos.
- O volume do próximo trade é reduzido dividindo `BaseVolume` por `LossReductionFactor` sempre que o trade anterior fechou com perda, assim como a redução de lotes no consultor especialista original.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `PipValue` | Valor monetário de um pip usado para transformar distâncias em pips em deslocamentos de preço. | `0.0001` |
| `StopLossPips` | Distância de stop-loss em pips. Definir como `0` para desabilitar o stop duro. | `50` |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como `0` para desabilitar o alvo duro. | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips. Quando `0`, nenhum trailing é aplicado. | `15` |
| `TrailingStepPips` | Passo mínimo em pips entre ajustes do trailing stop. Deve ser positivo quando o trailing está habilitado. | `5` |
| `PositionLivesBars` | Número máximo de barras completadas que uma posição pode permanecer aberta antes de ser forçada a fechar. | `4` |
| `CloseProfitsOnBar` | Quando maior que zero, posições lucrativas são fechadas após este número de barras desde a entrada. | `2` |
| `OpenWithinMinutes` | Quantidade máxima de minutos após a abertura da barra quando novos trades são permitidos. | `7` |
| `CandleSizeMinPips` | Comprimento de pavio necessário (em pips) no lado dominante do candle. | `15` |
| `OppositeShadowMaxPips` | Tamanho máximo (em pips) da sombra do candle oposta. | `1` |
| `StartHour` | Hora de início da sessão em horário da bolsa (0–23). | `6` |
| `EndHour` | Hora de fim da sessão em horário da bolsa (0–23). | `18` |
| `LossReductionFactor` | Divisor aplicado a `BaseVolume` após um trade perdedor. | `1.5` |
| `BaseVolume` | Tamanho padrão de ordem de mercado usado para entradas. | `1` |
| `CandleType` | Série de candles usada para os cálculos. O padrão é um período de 5 minutos. | `5 min` |

## Notas
- Sempre ajustar `PipValue` para corresponder ao tamanho do tick do instrumento (por exemplo `0.01` para cruzamentos JPY ou `1` para futuros de índice).
- Como a estratégia trabalha com candles completados, as execuções ocorrerão no fechamento da barra. Períodos menores (1–5 minutos) replicam melhor o comportamento intrabar do consultor especialista original.
- Nenhum indicador externo é necessário, tornando a estratégia fácil de executar em qualquer fonte de dados StockSharp.
