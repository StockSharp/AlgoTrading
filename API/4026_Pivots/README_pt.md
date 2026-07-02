# 4026 – Estratégia de Pivôs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia transporta os MetaTrader 4 arquivos localizados em `MQL/8550` (o indicador **Pivots** e o consultor especialista `Pivots_test` que o acompanha) para o `Strategy` API de alto nível de StockSharp. Ele mantém o comportamento original de calcular os níveis diários de pivô mínimo, organizando um par de ordens pendentes opostas no pivô central e gerenciando cada posição resultante com um stop-loss, take-profit e stop móvel fixos.

## Cálculo dinâmico

1. A estratégia assina um *período de pivô* configurável (`PivotCandleType`, diariamente por padrão).
2. Sempre que uma vela desse período termina, ela deriva os níveis clássicos de pivô dos preços OHLC do dia anterior:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`
   - `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)` e `S2 = Pivot − (High − Low)`
   - `R3 = 2 × Pivot + High − 2 × Low` e `S3 = 2 × Pivot − (2 × High − Low)`
3. Os níveis ficam ativos no início da próxima sessão. Quando isso acontece a estratégia registra os valores através de `AddInfoLog` (por exemplo: `Pivot levels for 2024-04-05: P=1.0924, R1=1.0956, …`).

## Fluxo de trabalho de pedidos pendentes

Uma vez ativos os níveis de pivô, a estratégia garante continuamente a existência de duas ordens pendentes ao preço de pivô:

- **Limite de compra** @ `Pivot` com proteção pós-preenchimento `SellStop` (stop-loss) em `S2` e `SellLimit` (take-profit) em `R2`.
- **Sell Stop** @ `Pivot` com proteção pós-preenchimento `BuyStop` em `R2` e `BuyLimit` em `S2`.

Todos os pedidos são enviados por meio dos métodos auxiliares de alto nível `BuyLimit`, `SellStop`, `SellLimit` e `BuyStop`. Se uma ordem for preenchida, o código recalcula o preço médio de entrada para aquela direção, cancela as ordens de proteção existentes e envia um novo par stop/limit que cobre todo o volume aberto (espelhando o comportamento MetaTrader onde cada posição herda a mesma proteção S2/R2). Se o stop de proteção ou o take-profit forem executados, os auxiliares relacionados serão compensados ​​automaticamente.

A estratégia usa uma única posição líquida, portanto, preenchimentos opostos se compensarão (ao contrário da cobertura baseada em tickets de MetaTrader). Este é o único desvio intencional do perito original.

## Lógica de parada móvel

- `TrailingStopPoints` define a distância em pontos indicadores (multiplicada pelo instrumento `PriceStep`).
- Para posições longas, o trailing stop é ativado quando o preço se move mais do que aquela distância acima da entrada média. O protetor `SellStop` é então movido para mais perto do mercado.
- Para posições curtas, a lógica de espelho se aplica, diminuindo o `BuyStop` à medida que o preço se move favoravelmente.
- As atualizações finais são orientadas pela série intradiária selecionada por meio de `CandleType` (velas de 15 minutos por padrão).

## Parâmetros

| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume de cada ordem pendente (lotes/contratos). | `0.1` |
| `TrailingStopPoints` | Distância de parada final em pontos. `0` desativa a lógica final. | `30` |
| `CandleType` | Série de velas intradiárias usadas para rastrear e manter a programação da sessão. | `15m` prazo |
| `PivotCandleType` | Período usado para calcular os níveis de pivô diários. | `1D` prazo |
| `LogPivotUpdates` | Quando `true`, os níveis pivô são gravados no registro de estratégia sempre que mudam. | `true` |

Todos os parâmetros numéricos são expostos por meio de `StrategyParam<T>` para que possam ser otimizados dentro da infraestrutura StockSharp.

## Registro e diagnóstico

- As atualizações dinâmicas são roteadas por meio de `AddInfoLog`, que substitui a saída MetaTrader `Comment`/`ObjectSetText`.
- O gerenciamento de ordens de proteção, o tratamento de posições e a lógica de rastreamento dependem exclusivamente dos ajudantes de alto nível de StockSharp; nenhum registro de pedido de baixo nível ou buffers de indicador são usados.

## Notas de uso

1. Anexe a estratégia a um conector que forneça velas diárias e intradiárias para o título escolhido.
2. Ajuste a etapa do instrumento, se necessário (`PriceStep` é detectado automaticamente; o substituto é `0.0001`).
3. Opcionalmente, ajuste `OrderVolume`, `TrailingStopPoints` ou os tipos de velas para corresponder à configuração original do MT4.

Nenhuma versão do Python é fornecida para esta porta conforme solicitado.
