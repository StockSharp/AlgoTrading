# NTK_07 Estratégia de Grade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia NTK_07 é uma grade de ordens pendentes simétrica originalmente escrita para MetaTrader 4. Ela coloca um par de ordens de stop em torno do preço atual e gerencia uma pirâmide de posição estilo martingale usando espaçamento configurável, stop loss, takeprofit e regras de trailing. A porta StockSharp mantém o comportamento original enquanto expõe cada configuração como um parâmetro de estratégia fortemente tipado.

A estratégia garante continuamente que:

* Um stop de compra e um stop de venda ficam estacionados no mercado quando não há ordens ativas.
* Depois que um rompimento é preenchido, a ordem pendente oposta é cancelada para evitar hedge.
* Pedidos adicionais na mesma direção podem ser adicionados em `Multiplier` vezes o tamanho anterior até que `LotLimit` seja excedido.
* Quando nenhuma escala adicional é permitida, a posição ativa é protegida por um trailing stop e, opcionalmente, um take-profit estendido dinamicamente.
* As ordens protetoras de stop e take-profit são recriadas automaticamente sempre que os volumes ou preços-alvo mudam, para que toda a posição aberta sempre compartilhe os mesmos níveis de saída.

## Lógica de negociação

1. **Filtro de sessão.** A negociação é ignorada aos sábados e domingos ou quando a hora atual estiver fora de `[StartHour, EndHour]`. O intervalo de horas corresponde à lógica MT4 original: `EndHour = 24` permite negociação durante todo o dia.
2. **Verificação de capital.** Quando um portfólio é anexado, o valor da conta corrente deve ser de pelo menos `MinCapital` antes de qualquer pedido ser criado.
3. **Quebra do canal (opcional).** Se `ChannelPeriod` for maior que zero, a máxima mais alta e a mínima mais baixa das últimas `ChannelPeriod` velas concluídas serão rastreadas. Dependendo de `UseChannelCenter`:
   * `false` – ambas as ordens pendentes são enviadas somente se o preço de venda estiver fora da faixa detectada (negociação de breakout).
   * `true` – os pedidos são enviados quando o preço volta ao ponto médio da faixa (estilo de reversão à média).
4. **Ordens pendentes iniciais.** Quando não há ordens ativas, um stop de compra é colocado `NetStepPips` acima do melhor pedido e um stop de venda `NetStepPips` abaixo do melhor lance. O volume base é definido pelo módulo de gestão de dinheiro.
5. **Escalonamento de posição.** Depois que uma ordem é preenchida, a ordem pendente oposta é cancelada. Se outra ordem já estiver ativa na mesma direção, a próxima ordem pendente será colocada a `NetStepPips` usando `RoundVolume(previousVolume × Multiplier)`. Quando o próximo volume exceder o `LotLimit` calculado, a estratégia para de adicionar à grade.
6. **Stop Loss e Take Profit.** Cada vez que a posição aberta muda, a estratégia recria um stop protetor e (opcionalmente) uma ordem Take-Profit para a exposição agregada longa ou curta. As distâncias são derivadas de `StopLossPips` e `TakeProfitPips`.
7. **Lógica de ponto de equilíbrio.** Quando `UseBreakEven = true` e o preço se movem `BreakEvenOffsetPips` além da última ordem preenchida, o stop loss é movido para o preço de entrada médio ponderado pelo volume (arredondado usando `PriceRoundingFactor`).
8. **Comportamento de trailing.** Se a próxima etapa de escala não for permitida, a estratégia usa o preço de vela mais alto/mais baixo para mover o stop em direção ao mercado em `TrailingStopPips`. Quando `TrailProfit = true` a distância do take-profit também é alterada, de modo que sempre permanece a `TakeProfitPips` de distância do último extremo da vela. Quando `UseMovingAverageFilter = true` e o preço são negociados contra a média móvel, a distância final é cortada pela metade, emulando o comportamento original de meio passo em torno de uma média móvel.

## Gestão de capital

A porta suporta as três regras originais de gerenciamento de dinheiro por meio do parâmetro `ManagementMode`:

| Modo | Descrição |
| ---- | ----------- |
| `Fixed` | Use `InitialLot` para cada novo pedido e limite o tamanho por pedido em `LotLimit`. |
| `BalanceBased` | Recalcular o lote inicial a partir do saldo da carteira: `ceil(balance / 1000 × PercentRisk / 100)`. O resultado é repetidamente dividido por `Multiplier` para projetar a menor ordem da grade, arredondada por `LotRoundingFactor`. O `LotLimit` original torna-se o tamanho máximo teórico do lote. |
| `Progressive` | Mantenha `InitialLot` como o volume base, mas projete a maior ordem teórica multiplicando por `Multiplier` para cada nível da grade. |

Todos os pedidos são arredondados usando `LotRoundingFactor` (padrão 10 => incrementos de 0,1), enquanto o preço de equilíbrio é arredondado com `PriceRoundingFactor` (padrão 10000 => incrementos de 0,0001).

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `NetStepPips` | 23 | Distância entre níveis de grade consecutivos. |
| `StopLossPips` | 115 | Distância de stop-loss aplicada a todas as posições. Defina como 0 para desativar. |
| `TakeProfitPips` | 300 | Distância de lucro para a posição agregada. Defina como 0 para desativar. |
| `TrailingStopPips` | 75 | Distância de parada móvel ativada quando a escala não for mais possível. |
| `Multiplier` | 1.7 | Multiplicador de volume para o próximo nível de grade. |
| `TrailProfit` | `true` | Quando ativado, o take-profit é deslocado ao longo do trailing stop. |
| `ManagementMode` | `Progressive` | Regra de gerenciamento de dinheiro selecionada. |
| `InitialLot` | 1 | Volume básico do pedido. |
| `LotLimit` | 7 | Tamanho máximo de lote permitido para uma única ordem pendente. |
| `MaxTrades` | 4 | Número máximo de níveis de grade. |
| `PercentRisk` | 10 | Porcentagem do saldo usada na gestão de dinheiro baseada no saldo. |
| `MinCapital` | 5.000 | Valor mínimo do portfólio exigido antes da negociação. |
| `UseBreakEven` | `false` | Habilite ajustes de ponto de equilíbrio. |
| `BreakEvenOffsetPips` | 5 | Limite de lucro (em pips) necessário para o ponto de equilíbrio. |
| `UseMovingAverageFilter` | `false` | Ativa a lógica de rastreamento com reconhecimento de média móvel. |
| `MovingAverageLength` | 100 | Comprimento da média móvel usada no filtro. |
| `MovingAverageShift` | 0 | Shift aplicado à média móvel (valores de velas anteriores são usados quando > 0). |
| `StartHour` | 0 | Horário de negociação permitido mais cedo (0–23). |
| `EndHour` | 24 | Último horário de negociação permitido (inclusive). |
| `ChannelPeriod` | 0 | Janela de lookback para o filtro de divisão/centro. Defina como 0 para desativar o filtro. |
| `UseChannelCenter` | `false` | Alternar entre entradas de estilo breakout (`false`) e ponto médio (`true`). |
| `LotRoundingFactor` | 10 | Divisor usado ao arredondar volumes. |
| `PriceRoundingFactor` | 10.000 | Divisor usado para arredondar o preço de equilíbrio. |
| `CandleType` | Período de 15 minutos | Tipo de vela funcional para detecção de alcance e cálculos de rastreamento. |

## Notas de implementação

* As carteiras de pedidos são assinadas para obter os melhores valores de compra/venda precisos antes de colocar pedidos pendentes. Quando o livro não está disponível, a estratégia volta ao preço de fechamento da vela.
* Paradas e alvos de proteção são recriados em vez de modificados, porque o API de alto nível expõe auxiliares mais seguros para registrar novos pedidos em vez de alterar os existentes.
* Os valores de mudança da média móvel além do histórico disponível retornam ao valor mais recente, evitando referências nulas enquanto mantêm o comportamento próximo à implementação MetaTrader.
* Todos os cálculos de preços são normalizados através de `Security.ShrinkPrice` para que os níveis de stop e limite sempre respeitem o tamanho do tick do instrumento.

## Dicas de uso

1. Configure `Strategy.Volume` para definir o multiplicador de tamanho de negociação nocional se sua corretora exigir escalonamento em relação ao tamanho do portfólio.
2. Ao testar símbolos com tamanhos de ticks exóticos, ajuste `LotRoundingFactor` e `PriceRoundingFactor` de acordo para que as operações de arredondamento permaneçam significativas.
3. Os parâmetros padrão foram retirados do EA original para dados EURUSD H1 entre 01/01/2008 e 01/11/2008. A reotimização é recomendada para outros ativos ou prazos.
4. Como a grade pode acumular uma grande exposição direcional, monitore sempre os valores `LotLimit` e `MaxTrades` para manter o risco sob controle.
