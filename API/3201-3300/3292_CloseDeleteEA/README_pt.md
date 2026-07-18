# Estratégia CloseDeleteEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia CloseDeleteEA reproduz o utilitário do MetaTrader que fecha posições em massa e remove ordens pendentes. Ela varre periodicamente o portfólio selecionado e envia ordens a mercado ou solicitações de cancelamento de acordo com filtros definidos pelo usuário. Isso a torna útil para liquidação emergencial ou cenários de limpeza quando o gerenciamento manual de ordens é lento demais.

## Recursos principais
- Fecha exposição comprada e/ou vendida com ordens a mercado.
- Cancela ordens pendentes que correspondem aos filtros configurados.
- Filtros opcionais de lucro/perda para evitar tocar posições específicas.
- Restringe a varredura ao ativo atual ou processa todo o portfólio.
- Filtra posições e ordens por identificador de estratégia.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CloseBuyPositions` | Fecha exposição comprada que corresponde aos filtros. |
| `CloseSellPositions` | Fecha exposição vendida que corresponde aos filtros. |
| `CloseMarketPositions` | Habilita o módulo de fechamento de posições de mercado. |
| `CancelPendingOrders` | Habilita o cancelamento de ordens pendentes. |
| `CloseOnlyProfitable` | Fecha posições apenas quando o PnL atual é não negativo. |
| `CloseOnlyLosing` | Fecha posições apenas quando o PnL atual é não positivo. |
| `ApplyToCurrentSecurity` | Quando true, apenas o ativo da estratégia é varrido. Caso contrário, todos os ativos do portfólio são processados. |
| `TargetStrategyId` | Filtro opcional de identificador de estratégia (valor vazio corresponde a tudo). |
| `TimerInterval` | Frequência do temporizador usada para o loop de gestão. |

## Notas de uso
1. Anexe a estratégia a um conector com um portfólio atribuído.
2. Opcionalmente configure filtros antes de iniciar a estratégia.
3. Inicie a estratégia para acionar o ciclo close/delete. A estratégia para automaticamente quando não restam posições ou ordens correspondentes.
4. Lembre-se de que solicitações de cancelamento só podem atingir ordens visíveis para a estratégia por meio do conector.

## Diferenças em relação à versão MQL
- O StockSharp trabalha com posições agregadas, então o controle individual por ticket é substituído por gestão de exposição líquida baseada em volume.
- A filtragem por id de estratégia imita o conceito original de magic number.
- Elementos visuais de limpeza de gráfico do MetaTrader não são reproduzidos.
