# Estratégia Scalper Multi Lote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Multi Lot Scalper** é um sistema de cálculo de média estilo martingale convertido do clássico consultor especialista MetaTrader "Multi Lot Scalper". O algoritmo original foi projetado para os principais pares de câmbio e baseou-se na inclinação do histograma MACD para decidir se o mercado está entrando em uma fase de alta ou de baixa. Uma vez identificada uma direção, a estratégia abre uma escada de ordens de mercado, aumentando progressivamente o volume após cada movimento adverso. A porta StockSharp mantém a lógica de entrada original, regras de gerenciamento de dinheiro e mecanismos de proteção enquanto aproveita a assinatura de vela de alto nível API.

A estratégia funciona melhor em instrumentos líquidos onde os spreads são reduzidos e a definição de pip é estável. Por padrão ele assina velas de 15 minutos, mas qualquer outro timeframe compatível com os instrumentos pode ser fornecido através do parâmetro `CandleType`.

## Lógica de negociação

1. **Detecção de sinal** – Um indicador MACD (`MacdFastLength`, `MacdSlowLength`, `MacdSignalLength`) é avaliado em cada vela finalizada. Quando a linha principal MACD sobe em relação ao valor anterior, a estratégia procura oportunidades longas, caso contrário, prepara-se para vender. O parâmetro `ReverseSignals` inverte essa interpretação para usuários que preferem entradas contrárias.
2. **Entrada inicial** – A primeira posição em uma nova sequência é aberta imediatamente após um sinal válido, desde que o filtro de data/hora (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`, `EndHour`, `EndMinute`) permita a negociação. São utilizadas ordens de mercado, espelhando a implementação MetaTrader.
3. **Pirâmide** – As ordens subsequentes são acionadas somente se o preço se mover em relação ao último preenchimento em pelo menos `EntryDistancePips`. Cada negociação adicional multiplica o volume base por 2 ou por 1,5 (quando `MaxTrades` está acima de 12) para reproduzir o tamanho do martingale do EA.
4. **Stops e metas** – `InitialStopPips` e `TakeProfitPips` são convertidos em níveis de preço para toda a cesta. Um trailing stop é ativado após o movimento a favor exceder `EntryDistancePips + TrailingStopPips`, estreitando a saída à medida que o mercado acelera.
5. **Proteção da conta** – Quando a cesta está perto de sua capacidade (`MaxTrades - OrdersToProtect`) e o lucro flutuante atinge `SecureProfit`, a estratégia fecha a negociação mais recente e bloqueia temporariamente novas entradas se `UseAccountProtection` estiver ativado.

## Gestão de capital

O consultor especialista original recalculou opcionalmente o tamanho do lote base em função do saldo da conta. A porta StockSharp mantém esse comportamento por meio dos parâmetros `UseMoneyManagement`, `RiskPercent` e `IsStandardAccount`. Quando o recurso está ativo, o lote base (`LotSize`) é ignorado e, em vez disso, derivado do valor do portfólio, dimensionado para contas mini ou padrão, assim como o código MQL.

## Parâmetros

| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `TakeProfitPips` | Distância de lucro aplicada a cada entrada, expressa em pips. | `40` |
| `LotSize` | Tamanho do lote base usado quando o gerenciamento de dinheiro está desativado. | `0.1` |
| `InitialStopPips` | Distância inicial do stop-loss em pips. | `0` |
| `TrailingStopPips` | Distância de parada móvel que é ativada após o limite. | `20` |
| `MaxTrades` | Número máximo de entradas de martingale permitidas simultaneamente. | `10` |
| `EntryDistancePips` | Movimento adverso mínimo antes de adicionar uma nova ordem. | `15` |
| `SecureProfit` | Lucro flutuante (em moeda) necessário para acionar a proteção da conta. | `10` |
| `UseAccountProtection` | Permite fechar a última negociação quando o limite de lucro seguro for atingido. | `true` |
| `OrdersToProtect` | Número de negociações finais afetadas pela regra do lucro seguro. | `3` |
| `ReverseSignals` | Inverte a interpretação MACD (a alta torna-se curta, a baixa torna-se longa). | `false` |
| `UseMoneyManagement` | Permite o cálculo de lote com base no saldo da conta. | `false` |
| `RiskPercent` | Percentagem de risco utilizada quando a gestão de dinheiro está ativa. | `12` |
| `IsStandardAccount` | Usa escala de lote padrão em vez de escala de minilote. | `false` |
| `EurUsdPipValue` | Substituição do valor do pip para EURUSD. | `10` |
| `GbpUsdPipValue` | Substituição do valor do pip para GBPUSD. | `10` |
| `UsdChfPipValue` | Substituição do valor do pip para USDCHF. | `10` |
| `UsdJpyPipValue` | Substituição do valor do pip para USDJPY. | `9.715` |
| `DefaultPipValue` | Valor do pip substituto usado para outros instrumentos. | `5` |
| `StartYear` | Primeiro ano civil em que novas vagas poderão ser abertas. | `2005` |
| `StartMonth` | Primeiro mês permitido para novas entradas. | `1` |
| `EndYear` | Último ano civil para iniciar negociações. | `2006` |
| `EndMonth` | Último mês civil para iniciar negociações. | `12` |
| `EndHour` | Hora (24h) após a qual novas entradas são bloqueadas. | `22` |
| `EndMinute` | Componente minuto do horário limite diário. | `30` |
| `CandleType` | Tipo de vela usado para geração de sinal (o padrão é 15 minutos). | `15-minute time frame` |
| `MacdFastLength` | Comprimento EMA rápido do indicador MACD. | `14` |
| `MacdSlowLength` | Comprimento lento de EMA do indicador MACD. | `26` |
| `MacdSignalLength` | Comprimento do sinal EMA do indicador MACD. | `9` |

## Diretrizes de uso

- Certifique-se de que o passo pip do instrumento corresponda à configuração do valor pip. Atualize os parâmetros de valor pip ao aplicar a estratégia a CFDs, metais ou ativos criptográficos.
- A escamação martingale pode aumentar a exposição rapidamente. Comece com valores conservadores `MaxTrades`, `EntryDistancePips` e `TrailingStopPips` antes de experimentar cestas maiores.
- Otimize as configurações de MACD e o intervalo de velas para o instrumento que está sendo negociado. Gráficos mais lentos geralmente reduzem o número de etapas médias, enquanto gráficos mais rápidos aumentam a atividade.
- A regra de proteção de contas é particularmente importante em mercados propensos a reversões repentinas. Se o lucro garantido for atingido com frequência, considere reduzir `SecureProfit` ou restringir `TrailingStopPips`.
- O filtro da janela de negociação permite que a estratégia seja desativada após um horário intradiário escolhido. Isto é útil para evitar comunicados de imprensa ou volatilidade no final da sessão.

## Notas de conversão

- A versão StockSharp usa a assinatura de velas de alto nível API (`SubscribeCandles().BindEx(...)`) em vez do processamento manual de ticks, mantendo o gerenciamento de indicadores transparente.
- Os trailing stops são tratados internamente gerenciando o nível de stop agregado da cesta, em vez de modificar cada pedido filho individualmente, o que reflete o comportamento pretendido em um ambiente com reconhecimento de portfólio.
- O uso de `AccountBalance` pelo EA para dimensionamento de posição é mapeado para a propriedade `Portfolio.CurrentValue`, mantendo a paridade entre as implementações de MetaTrader e StockSharp.
