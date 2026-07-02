# Estratégia Straddle Trail v2.40
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Straddle Trail v2.40** é uma versão StockSharp do MetaTrader 4 consultor especialista "Straddle&Trail" (versão 2.40). O algoritmo prepara um par simétrico de ordens de stop antes de um evento de alto impacto, gerencia automaticamente a posição acionada com lógica de ponto de equilíbrio e trailing-stop e pode reagir a negociações manuais que já existem na conta.

## Fluxo de trabalho principal

1. **Preparação**
   - A estratégia assina atualizações do livro de pedidos para acompanhar o melhor lance/venda e velas de minuto (configuráveis) para decisões de agendamento.
   - Os pips são calculados a partir das configurações do instrumento para que todas as distâncias definidas em pips sejam devidamente convertidas em preços.
2. **Colocação de straddle**
   - No lead time configurado antes do evento (`PreEventEntryMinutes`), ou imediatamente se `PlaceStraddleImmediately` estiver ativado, uma ordem buy-stop e uma ordem sell-stop são colocadas `DistanceFromPrice` pips acima e abaixo do mercado.
   - Antes do evento, os pedidos pendentes podem ser recentralizados a cada minuto se `AdjustPendingOrders` estiver ativado. Os ajustes param `StopAdjustMinutes` antes do evento.
3. **Gerenciamento de pedidos**
   - Uma vez acionado um lado, a remoção opcional da ordem pendente oposta (`RemoveOppositeOrder`) evita a dupla exposição.
   - `ShutdownNow` junto com `ShutdownOption` torna possível nivelar posições abertas e/ou cancelar ordens pendentes sob demanda.
4. **Proteção de posição**
   - Os níveis iniciais de stop-loss e take-profit são derivados dos parâmetros baseados em pip.
   - Quando o preço atinge o ponto de equilíbrio, o stop é movido para travar `BreakevenLockPips` de lucro.
   - O rastreamento começa imediatamente ou após o ponto de equilíbrio (dependendo de `TrailAfterBreakeven`).
   - Se `ManageManualTrades` for verdadeiro, quaisquer posições manuais detectadas pela estratégia serão protegidas usando as mesmas regras.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `ShutdownNow` | Força a execução da lógica de desligamento no próximo fechamento da vela. |
| `ShutdownOption` | Escolhe o que fechar: tudo, apenas posições acionadas, apenas posições longas, somente curtas, todas as ordens pendentes, apenas paradas de compra ou apenas paradas de venda. |
| `DistanceFromPrice` | Distância em pips entre o preço atual e as ordens stop pendentes. |
| `StopLossPips` | Distância inicial do stop-loss em pips. |
| `TakeProfitPips` | Distância inicial de lucro em pips. Defina como 0 para desativar o nível de lucro. |
| `TrailPips` | Distância do trailing-stop em pips. Defina como 0 para desativar o rastreamento. |
| `TrailAfterBreakeven` | Se for verdade, o rastreamento só começará depois que o gatilho do ponto de equilíbrio for atingido. |
| `BreakevenLockPips` | Lucro (em pips) bloqueado assim que o gatilho do ponto de equilíbrio for acionado. |
| `BreakevenTriggerPips` | Limite de lucro (em pips) que ativa o movimento de equilíbrio. |
| `EventHour` / `EventMinute` | Horário programado do evento de notícias (horário do corretor). Defina ambos como 0 para desabilitar a programação e usar o modo manual/imediato. |
| `PreEventEntryMinutes` | Minutos antes do evento quando o straddle é colocado. |
| `StopAdjustMinutes` | Minutos antes do evento, quando os ajustes de pedido param. O valor mínimo é 1 minuto. |
| `RemoveOppositeOrder` | Remove a ordem pendente oposta após um lado do straddle ser preenchido. |
| `AdjustPendingOrders` | Centraliza novamente as ordens pendentes a cada minuto até que a janela de ajuste de parada seja alcançada. |
| `PlaceStraddleImmediately` | Coloca o straddle assim que a estratégia começa, ignorando a programação do evento. |
| `ManageManualTrades` | Estende a lógica de ponto de equilíbrio e trailing para posições manuais. |
| `CandleType` | Série de velas usada para a lógica de temporização e agendamento (o padrão é o período de 1 minuto). |

## Notas de uso

- Sempre configure o tamanho correto do pip para o instrumento por meio das configurações de segurança para que as distâncias baseadas no pip se traduzam em preços com precisão.
- A estratégia fecha posições usando ordens de mercado quando uma condição de stop-loss ou take-profit é atendida, o que reflete como o EA original executou ajustes manuais de stop.
- Quando `PlaceStraddleImmediately` está desativado e a programação está ativa, o straddle é colocado apenas uma vez por dia de negociação. Redefina a estratégia para se preparar para outro evento no mesmo dia.
- Os controles de desligamento podem ser usados como um freio de emergência para nivelar rapidamente a exposição e remover ordens pendentes em vários cenários.

## Detalhes da conversão

- Todos os comentários no código foram traduzidos para o inglês e ampliados com explicações adicionais para maior clareza.
- Métodos StockSharp API de alto nível (`BuyStop`, `SellStop`, `ClosePosition`) são usados para manter a implementação próxima às práticas recomendadas da estrutura.
- O algoritmo evita pesquisas diretas de indicadores e, em vez disso, depende da vela vinculada e das assinaturas do livro de pedidos, conforme exigido pelas diretrizes do projeto.
