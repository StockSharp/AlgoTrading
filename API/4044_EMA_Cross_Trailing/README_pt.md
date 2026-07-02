# EMA Estratégia de rastreamento cruzado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é a conversão StockSharp do MetaTrader 4 consultor especialista localizado em `MQL/8606/EMA_CROSS_2.mq4`. Ele preserva a ideia original de rastrear a relação entre uma média móvel exponencial lenta e uma rápida e abrir uma posição de mercado única quando ocorre um cruzamento. As saídas de proteção (takeprofit, stop loss e trailing stop) são tratadas por meio do auxiliar `StartProtection` de alto nível para que o comportamento espelhe a implementação MetaTrader ao usar as práticas recomendadas StockSharp.

## Lógica de negociação
- Construa velas com o `CandleType` configurável (barras de 15 minutos por padrão) e alimente dois indicadores EMA: o EMA lenta usa `SlowEmaLength` e o EMA rápida usa `FastEmaLength`.
- Mantenha a última direção do EMA lenta em relação ao EMA rápida. A primeira vela concluída após a formação de ambos os indicadores é usada apenas para inicializar esta direção, assim como a guarda `first_time` no consultor original.
- Quando o EMA lenta se mover acima do EMA rápida (a nova direção se torna `1`) e a estratégia for plana, envie uma ordem de compra de mercado. Quando o EMA lento se move abaixo do EMA rápido (a nova direção se torna `2`) e a estratégia é plana, envie uma ordem de venda a mercado. Isso reproduz o mapeamento exato para cima/para baixo da função MQL `Crossed(LEma, SEma)`.
- Apenas uma posição pode estar ativa por vez. Enquanto uma negociação está aberta (ou a ordem de entrada ainda está pendente), cruzamentos adicionais são ignorados.

## Gestão comercial e de risco
- `StartProtection` configura distâncias de take-profit, stop loss e trailing stop em unidades de preço calculadas a partir do instrumento `PriceStep`. As paradas finais são opcionais: defina `TrailingStopPips` como zero para desativá-las.
- As ordens são colocadas com `BuyMarket`/`SellMarket` e fechadas pelo mercado quando qualquer nível de proteção é acionado, exatamente como o `OrderSend` e a lógica final do consultor original.
- O tamanho do lote base é controlado por `OrderVolume`. Antes de cada entrada ele é alinhado ao passo de volume do instrumento, mínimo e máximo para evitar rejeição.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância em pips (etapas de preço) usada para o take-profit protetor. Padrão: 20. |
| `StopLossPips` | Distância em pips usada para o stop loss de proteção. Padrão: 30. |
| `TrailingStopPips` | Distância final em pips. Defina como `0` para desativar o rastreamento. Padrão: 50. |
| `OrderVolume` | Tamanho do lote das entradas no mercado antes do alinhamento. Padrão: 2. |
| `FastEmaLength` | Período do EMA rápida aplicado aos preços de fechamento. Padrão: 5. |
| `SlowEmaLength` | Período de lentidão EMA aplicado aos preços de fechamento. Padrão: 60. |
| `CandleType` | Prazo para construção de velas. Padrão: 15 minutos. |

## Notas
- A estratégia espera até que ambos os EMAs estejam totalmente formados antes de reagir a um cruzamento, removendo a verificação `Bars < 100` do script MQL e alcançando a mesma estabilidade.
- Como apenas são usadas ordens de mercado, não há chamadas `OrderModify` individuais. O módulo de proteção integrado reposiciona automaticamente o trailing stop da mesma forma que o loop MetaTrader atualizou `OrderStopLoss`.
- Nenhuma porta Python é fornecida (por solicitação); apenas a implementação do C# está incluída.
