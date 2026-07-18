# Estratégia TurnGrid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia TurnGrid** replica o comportamento do MQL5 Expert Advisor `TurnGrid.mq5` original. Ele constrói uma grade de preços simétrica em torno do preço de mercado atual e alterna entre pedidos longos e curtos sempre que o preço migra de uma célula da grade para outra. A estratégia reequilibra continuamente as ordens abertas para manter a exposição de alta e de baixa até que a meta de capital configurada seja alcançada.

A conversão usa o API de alto nível de StockSharp: as assinaturas de velas impulsionam as atualizações da grade, as ordens de mercado tratam de entradas e saídas e o gerenciamento de risco é expresso por meio de parâmetros de estratégia. Todos os comentários foram traduzidos para o inglês e a nomenclatura segue as convenções StockSharp.

## Lógica de negociação

1. Quando a estratégia é iniciada, ela captura o último fechamento da vela e constrói uma grade contendo `4 * GridShares` níveis. O nível central é definido para o preço atual, os níveis superiores são escalonados em `1 + GridDistance` e os níveis inferiores são escalonados em `1 - GridDistance`.
2. Uma ordem inicial de compra de mercado é colocada no centro da grade. Seu volume é calculado a partir da parcela do orçamento disponível (`Balance / GridShares`) e de uma fórmula de participação incremental herdada da versão MQL.
3. Cada vela finalizada atualiza o índice da grade atual com base no preço de fechamento. Se o índice mudar:
   - As posições vinculadas aos tickets a dois níveis do novo índice são fechadas (os tickets comprados abaixo do preço são vendidos, os tickets vendidos acima são recomprados).
   - Novas posições são abertas para manter as âncoras longas e curtas no nível ativo. Se nenhum dos lados estiver presente, a estratégia abre o lado com menos posições ativas para equilibrar a exposição.
4. As taxas são aproximadas por meio do parâmetro `FeeRate`. Cada pedido atendido contribui para uma taxa total usada na avaliação de desempenho.
5. Quando o patrimônio da conta (após subtrair a estimativa de taxa acumulada) excede o saldo inicial em `EquityTakeProfit`, a estratégia fecha a posição líquida e reconstrói a grade em torno do preço mais recente.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `GridDistance` | Distância relativa entre níveis de grade adjacentes. | `0.01` |
| `GridShares` | Número máximo de posições de grade simultâneas que podem estar ativas. | `50` |
| `EquityTakeProfit` | Ganho percentual sobre o saldo inicial necessário para zerar a rede. | `0.02` |
| `FeeRate` | Taxa de transação estimada por negociação, aplicada ao volume executado. | `0.0008` |
| `CandleType` | Série de velas usada para impulsionar a estratégia. | Período de `1` minutos |

## Notas de implementação

- A assinatura de velas é gerenciada por meio de `SubscribeCandles(CandleType)` e a estratégia reage apenas às velas finalizadas, correspondendo à lógica orientada por ticks do EA original, mantendo a compatibilidade com StockSharp.
- O estado da grade é armazenado em uma matriz leve de estruturas `GridLevel` contendo âncoras de preços, sinalizadores booleanos e volumes de tickets para fechamentos adiados.
- Os tamanhos dos pedidos seguem a fórmula original de alocação de capital incremental, com normalização adicional por meio das configurações `VolumeStep`, `VolumeMin` e `VolumeMax` do título.
- As redefinições baseadas em ações aguardam o fechamento da posição líquida atual antes de reconstruir a rede, garantindo transições limpas entre os ciclos de negociação.

## Arquivos

- `CS/TurnGridStrategy.cs` – implementação em C# da estratégia usando StockSharp construções de alto nível.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – Documentação em chinês simplificado.
- `README_ru.md` – Documentação russa.
