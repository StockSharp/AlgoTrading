# Estratégia de Open Close
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Open Close é um port do assessor especialista MetaTrader 5 `Open Close.mq5` (ticket 23090). A estratégia observa a relação entre as aberturas e fechamentos das duas velas finalizadas mais recentes. Negocia uma única posição por vez: quando a vela mais nova se reverte em relação à anterior, a estratégia entra; e quando ambas as velas apontam na mesma direção, ela sai. A versão em C# reproduz o dimensionamento adaptativo de lote original que reduz a exposição após uma série de operações perdedoras.

## Lógica da estratégia
### Filtro de padrão de velas
* A estratégia trabalha exclusivamente com velas completadas fornecidas pelo parâmetro configurável `CandleType`.
* Mantém uma janela deslizante das duas últimas velas finalizadas (denominadas `previous` e `older`).
* O padrão compara tanto as aberturas quanto os fechamentos dessas velas:
  * **Reversão altista** – `previous.Open > older.Open` **e** `previous.Close < older.Close`.
  * **Reversão baixista** – `previous.Open < older.Open` **e** `previous.Close > older.Close`.

### Regras de entrada
* Se nenhuma posição estiver aberta e o padrão de reversão altista aparecer, a estratégia envia uma ordem de compra de mercado.
* Se nenhuma posição estiver aberta e o padrão de reversão baixista aparecer, envia uma ordem de venda de mercado.
* Apenas uma posição é permitida. Sinais opostos são ignorados até que a negociação ativa seja fechada.

### Regras de saída
* Quando uma posição longa é mantida, a estratégia sai se as duas velas rastreadas se moverem para baixo (`previous.Open < older.Open` e `previous.Close < older.Close`).
* Quando uma posição curta é mantida, o gatilho de saída é simétrico (`previous.Open > older.Open` e `previous.Close > older.Close`).
* Não há ordens de stop-loss ou take-profit no assessor original, portanto o port depende exclusivamente da relação de velas para fechar negociações.

### Dimensionamento de posição e tratamento de série de perdas
* O volume da ordem é determinado principalmente por `MaximumRiskPercent` – a fração desejada do valor do portfólio investida por negociação. O tamanho bruto é `Portfolio.CurrentValue × MaximumRiskPercent ÷ referencePrice` usando o último fechamento como proxy de preço.
* Se a valoração do portfólio ou o preço não estiver disponível, o parâmetro `FallbackVolume` atua como padrão seguro.
* Após cada negociação completamente fechada, o PnL realizado é armazenado. A série de perdas consecutivas é contada nos últimos `HistoryDays` dias.
  * Quando a série é maior que uma negociação, o tamanho da próxima ordem é reduzido por `volume × losses ÷ DecreaseFactor`, imitando a lógica do MT5.
* O volume final respeita o step de volume do instrumento, bem como os limites mínimo e máximo de volume.

### Notas adicionais de implementação
* A estratégia reage apenas a `CandleStates.Finished`, garantindo que o padrão use dados de mercado completos.
* As verificações de entrada e saída ocorrem no fechamento da vela mais nova. No MetaTrader, a ordem é enviada na abertura da próxima barra; a diferença é insignificante para períodos mais altos, mas deve ser considerada para intervalos muito curtos.
* As métricas de portfólio no StockSharp aproximam as informações de conta do MetaTrader. Ajuste `MaximumRiskPercent` ou `FallbackVolume` se o broker usar multiplicadores de contrato diferentes.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `MaximumRiskPercent` | `decimal` | `0.02` | Fração do valor do portfólio usada para dimensionar uma nova posição (0.02 = 2%). |
| `DecreaseFactor` | `decimal` | `3` | Divisor aplicado ao tamanho do lote após negociações perdedoras consecutivas. Valores maiores suavizam a redução. |
| `HistoryDays` | `int` | `60` | Número de dias de calendário verificados ao contar a série de perdas mais recente. |
| `FallbackVolume` | `decimal` | `0.1` | Volume de ordem usado quando o cálculo baseado em risco não pode ser realizado. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Série de velas que fornece os valores de abertura/fechamento para geração de sinais. |

## Diferenças em relação à versão MetaTrader
* As verificações de margem de conta dependem do `Portfolio.CurrentValue` do StockSharp; o MetaTrader usava `AccountFreeMargin`. O comportamento corresponde à regra de risco original apenas quando ambas as plataformas reportam valorações similares.
* O histórico de negociações é coletado das próprias execuções da estratégia em vez do histórico de toda a terminal. Certifique-se de que a estratégia funcione por tempo suficiente para acumular estatísticas de série.
* O port mantém o modelo de posição única (sem pirâmide) e reflete a falta original de ordens protetoras. Adicione stops externamente se necessário para controle de risco.
