# Estratégia Flutuante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Flutuante** é um port do StockSharp do expert advisor do MetaTrader "Fluctuate". Reproduz o comportamento similar a uma grade do original usando a API de alto nível: uma assinatura de velas impulsiona todas as decisões, as entradas no mercado são realizadas com `BuyMarket` / `SellMarket`, e as ordens de recuperação são colocadas com stop orders. A exposição comprada e vendida é rastreada separadamente para imitar a contabilidade de posição estilo hedging usada no MetaTrader, enquanto a posição real do StockSharp permanece líquida.

## Ideia central

1. Cada vez que uma nova vela fecha, a estratégia compara os dois últimos preços de fechamento. Um fechamento mais alto abre uma compra a mercado, um fechamento mais baixo abre uma venda a mercado. Se ambos os fechamentos forem iguais a barra é ignorada.
2. Cada posição executada recebe um stop-loss e take-profit fixos (expressos em pips). A estratégia também registra o preço exato de execução e o volume líquido adicionado pelo trade.
3. Após uma entrada, uma stop order **oposta** é ativada a `StepPips` de distância do último preço de execução (mais um pequeno buffer de spread). Seu volume é derivado do trade anterior e do `LotCoefficient`, opcionalmente usando a exposição acumulada quando `MultiplyLotCoefficient = true`.
4. Quando a stop order é ativada, ela cancela a ordem pendente anterior, atualiza as estatísticas de exposição interna e imediatamente programa uma nova stop order de recuperação na outra direção. Isso reproduz o loop de averaging/martingale presente na implementação MQL.
5. A proteção de trailing eleva (ou abaixa) o stop uma vez que o preço se move pelo menos `TrailingStopPips + TrailingStepPips` a favor da posição. Isso emula o EA original que exigia um buffer de lucro adicional antes de apertar o stop.

## Fluxo de trading

- **Detecção de sinais.** O feed de velas é assinado via `SubscribeCandles`. Apenas velas terminadas são processadas. A estratégia se recusa a negociar fora da janela de tempo `[StartHour, EndHour)` ou quando o guardião de capital é ativado.
- **Dimensionamento inicial de posição.** Dependendo de `PositionSizingMode`, o primeiro trade em uma sequência usa um lote fixo (`FixedVolume`) ou um lote baseado em risco (`RiskPercent`). No modo de risco, o risco permitido (porcentagem do capital atual) é dividido pela perda monetária que ocorreria se o stop-loss fosse ativado. Passo de preço e preço de passo são usados para converter pips em moeda.
- **Contabilidade de exposição.** Acumuladores separados rastreiam volume comprado e vendido, preço médio e o preço extremo atingido desde a entrada. Isso permite que a estratégia mantenha ambos os lados "abertos" internamente, embora o StockSharp use netting.
- **Ordens de recuperação.** Após cada execução, o algoritmo calcula o volume da próxima stop order:
  - Quando `MultiplyLotCoefficient = false`, o novo volume equivale a `LastVolume × LotCoefficient`.
  - Quando `true`, a exposição absoluta total é multiplicada por `LotCoefficient`.
  - O volume é normalizado para as restrições da bolsa (passo, volume mínimo e máximo) e rejeitado quando excederia `MaxTotalVolume` ou o número de posições ativas mais ordens excederia `MaxPositions`.
- **Alvo de lucro e guardião de capital.** PnL não realizado agregado é calculado traduzindo diferenças de preço em moeda usando `PriceStep`/`StepPrice`. Se atingir `ProfitTarget`, todas as posições são fechadas e as ordens pendentes são canceladas. O trading também é suspenso quando o capital cai abaixo de `MinEquityPercent` do saldo inicial.
- **Lógica de trailing.** Para posições compradas, o preço mais alto visto desde a entrada é registrado. Uma vez que supera o preço de entrada em `TrailingStopPips + TrailingStepPips`, um trailing stop é definido `TrailingStopPips` atrás da máxima. Posições vendidas aplicam a regra simétrica com o preço mais baixo. As atualizações de trailing substituem o stop-loss fixo.

## Detalhes de gestão de risco

- **Stop / take profit.** Ambos são opcionais (definir o valor em pips como zero para desabilitar). Eles são recalculados para a exposição comprada ou vendida agregada sempre que um novo trade adiciona volume.
- **Máx. posições.** Conta o número de lados abertos (comprado + vendido) mais a stop order de recuperação ativa. Quando o limite é atingido, a estratégia se recusa a enviar novas stop orders.
- **Volume total máximo.** Limita a soma do volume aberto absoluto e o volume da ordem de recuperação ativa.
- **CloseAllAtStart.** Interruptor de segurança opcional para zerar o livro antes de a estratégia começar a negociar.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período principal usado para detecção de sinais. | Período de 1 minuto |
| `StopLossPips` | Distância entre o preço de entrada e o stop-loss (pips). `0` desativa o stop. | 50 |
| `TakeProfitPips` | Distância entre o preço de entrada e o take-profit (pips). `0` desativa o take-profit. | 50 |
| `TrailingStopPips` | Distância do trailing stop (pips). Requer `TrailingStepPips > 0`. | 5 |
| `TrailingStepPips` | Lucro adicional necessário antes que o trailing stop avance (pips). | 5 |
| `StepPips` | Distância entre o último preço de execução e o stop de recuperação oposto (pips). | 30 |
| `LotCoefficient` | Multiplicador aplicado ao volume anterior (ou exposição total). | 2.0 |
| `MultiplyLotCoefficient` | Quando `true`, o volume da nova ordem é calculado a partir da exposição total em vez do último trade. | `false` |
| `MaxPositions` | Número máximo de lados abertos simultâneos mais a ordem pendente ativa. | 9 |
| `MaxTotalVolume` | Limite para a soma do volume aberto e o volume da ordem de recuperação. | 50 |
| `ProfitTarget` | Lucro não realizado (em moeda da conta) que desencadeia uma saída completa. `0` desativa o alvo. | 50 |
| `MinEquityPercent` | Percentual mínimo de capital (vs. saldo inicial) necessário para continuar negociando. Abaixo deste limiar apenas saídas são permitidas. | 30 |
| `CloseAllAtStart` | Fechar todas as posições e cancelar ordens quando a estratégia inicia. | `false` |
| `StartHour` | Hora de início da janela de trading (inclusiva, horário da bolsa). | 10 |
| `EndHour` | Hora de fim da janela de trading (exclusiva, horário da bolsa). | 20 |
| `PositionSizingMode` | `FixedVolume` para lotes estáticos, `RiskPercent` para dimensionamento por percentual de capital. | `FixedVolume` |
| `VolumeOrRisk` | Tamanho de lote fixo (quando `FixedVolume`) ou percentual de risco (quando `RiskPercent`). | 1.0 |

## Notas de implementação

- Os preços da stop order usam uma aproximação de spread mínimo (`PriceStep` quando disponível) porque o MetaTrader exigia que a ordem estivesse fora do nível de congelamento. Ajustar `StepPips` se o spread real for mais amplo.
- A estratégia cancela qualquer ordem de recuperação restante sempre que um novo trade é executado. Isso corresponde ao EA original que excluía todas as ordens pendentes após uma execução.
- Como os portfólios do StockSharp são líquidos, a exposição com hedge é simulada internamente. A posição real do corretor sempre refletirá a quantidade líquida.
- O dimensionamento de posição baseado em risco requer valores válidos de `PriceStep` e `StepPrice` da descrição do instrumento.

## Dicas de uso

1. Selecionar um tipo de vela apropriado que corresponda ao período de teste do EA original (tipicamente M5 ou M15) para melhor fidelidade.
2. Verificar os limites de volume da bolsa: se o volume de recuperação normalizado se tornar zero, a estratégia deixará de adicionar novas pernas.
3. Quando `PositionSizingMode = RiskPercent`, garantir que o portfólio contenha informações de capital atualizadas; caso contrário a estratégia recorre ao tamanho de lote fixo.
4. Combinar com `StrategyProtection` incorporado do StockSharp (habilitado via `StartProtection()`) para adicionar salvaguardas adicionais no nível de conta, se necessário.
