# Grade do Gerenciador de Comércio XP (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia **XP Trade Manager Grid** é uma porta direta do MetaTrader 4 consultor especialista `XP Trade Manager Grid.mq4`. Ele automatiza uma grade simétrica que adiciona continuamente novas posições sempre que o mercado se afasta de um número configurável de pontos da última etapa preenchida. O especialista original administrou os lucros com níveis de take-profit parciais para as três primeiras ordens, um cluster de ponto de equilíbrio quando a escada cresce e uma proteção de risco global baseada na porcentagem da conta. A implementação StockSharp mantém as mesmas ideias enquanto aproveita primitivas API de alto nível (ordens de mercado, assinaturas de velas e parâmetros de estratégia).

## Lógica de negociação

1. **Entrada inicial** – a estratégia abre imediatamente a primeira ordem de mercado na direção selecionada pelo usuário (venda por padrão). Todas as negociações subsequentes são agrupadas na escada da grade.
2. **Expansão da grade** – sempre que o preço de fechamento oscila em `StepPoints` * passo de preço além da perna mais recente de um lado, uma nova ordem de mercado é colocada nessa direção, desde que o número total de pernas simultâneas seja inferior a `MaxOrders`.
3. **TP dedicado para as três primeiras etapas** – as três primeiras ordens de cada lado herdam suas compensações de take-profit exclusivas (`TakeProfit1Partitive`, `TakeProfit2`, `TakeProfit3`). Uma vez que os máximos/mínimos da vela tocam esses níveis, a perna fica achatada.
4. **Cluster de equilíbrio** – quando a quantidade total de pernas abertas atinge quatro ou mais, a estratégia calcula o preço de equilíbrio ponderado de toda a escada. Dependendo de qual lado tem mais pernas, ele compensa esse ponto de equilíbrio pela meta total correspondente (`TakeProfit4Total`… `TakeProfit15Total`) dividida entre os pedidos ativos. Se o preço atingir o objetivo calculado, toda a exposição será fechada.
5. **Renovação do ciclo** – se a primeira ordem de um ciclo for fechada, mas o lucro coletado em pontos ainda estiver abaixo de `TakeProfit1Total`, a lógica espera que o mercado se mova `TakeProfit1Offset` pontos além da última saída e então reabre a ordem inicial.
6. **Controle de risco** – o lucro flutuante na moeda da conta (realizado + não realizado) é constantemente comparado com `RiskPercent` por cento do saldo inicial do portfólio. Se o limite de perda for violado, toda a escada será achatada imediatamente.

A porta C# rastreia cada trecho preenchido internamente. Os preenchimentos parciais são suportados e as estruturas cobertas (compra e venda simultâneas) são resolvidas exatamente como no especialista MQL: os preenchimentos opostos primeiro cancelam as pernas pendentes antes que uma nova exposição seja registrada.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Tipo de dados usado para conduzir a estratégia (padrão: velas de 1 minuto). |
| `OrderVolume` | Volume de cada ordem/perna de mercado. |
| `MaxOrders` | Máximo de pernas simultâneas em ambas as direções. |
| `StepPoints` | Distância em pontos entre ordens de grade consecutivas. |
| `RiskPercent` | Perda flutuante máxima tolerável como % do saldo inicial da carteira. |
| `TakeProfit1Total` | Meta total de pontos acumulados pelos ciclos do pedido nº 1 antes que nenhuma renovação automática ocorra. |
| `TakeProfit1Partitive` | Distância de lucro (pontos) para a primeira etapa. |
| `TakeProfit1Offset` | Distância mínima de retração necessária antes de recriar a primeira ordem. |
| `TakeProfit2` / `TakeProfit3` | Deslocamentos de TP individuais (pontos) para as pernas 2 e 3. |
| `TakeProfit4Total` … `TakeProfit15Total` | Totais de TP de ponto de equilíbrio usados quando o tamanho da escada atinge o número correspondente de pedidos. |
| `InitialSide` | Direção do primeiro pedido (Compra ou Venda). |

> **Observação:** Todas as entradas baseadas em pontos são automaticamente dimensionadas pela segurança `PriceStep`, correspondendo à lógica `Point()` original de MetaTrader.

## Comportamento comparado à versão MetaTrader

* A variante StockSharp fecha as três primeiras etapas por meio de ordens de mercado em vez de modificar os valores de take-profit individuais, porque o API de alto nível não expõe a modificação direta da ordem.
* Os cálculos de lucro flutuante baseiam-se na etapa do instrumento e no preço da etapa. Corretores com especificações de contrato exóticas podem exigir ajustes se não exporem esses campos.
* Os rótulos de nível de plataforma mostrados no MT4 ("Profit pips" / "Profit coins") não são reproduzidos. Em vez disso, as estatísticas do ciclo interno são usadas para decidir quando reabrir o primeiro pedido.

## Requisitos

* Anexe a estratégia a uma segurança que exponha `PriceStep` e `StepPrice`.
* Certifique-se de que o conector de negociação suporta ordens de mercado imediatas ou canceladas. Todas as pernas da grade são executadas por meio de métodos auxiliares `BuyMarket`/`SellMarket`.

## Dicas de uso

1. Comece com valores `OrderVolume` pequenos ao testar para avaliar como a grade se comporta em seu feed.
2. Ajuste cuidadosamente `StepPoints` para a volatilidade do símbolo. Degraus maiores reduzem o número de pernas abertas e, portanto, o rebaixamento.
3. Aumente `TakeProfit1Offset` ao negociar instrumentos com spreads mais amplos para evitar reentradas prematuras.
4. Combine a estratégia com a chamada `StartProtection()` integrada, que monitora desconexões inesperadas e reconecta normalmente.
