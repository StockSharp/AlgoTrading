# Estratégia do bisturi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Bisturi** é uma versão StockSharp do MetaTrader 4 consultor especialista `Scalpel.mq4`. O sistema procura rompimentos de impulso no período de base, confirma o movimento com mínimos/máximos de períodos de tempo mais altos e filtra as entradas usando um estudo de volume direcional construído em velas de 1 minuto. O gerenciamento de posições reflete o EA original: os lucros são colhidos com um take-profit fixo que diminui com o tempo, o stop-loss pode ser rastreado assim que o preço se move a favor da negociação e todas as posições podem ser fechadas à força após um período de vida configurável ou na noite de sexta-feira.

## Lógica de negociação
- **Filtro de tendência de vários períodos de tempo**: um sinal longo requer que os mínimos atuais nas velas H4, H1 e M30 sejam maiores que os mínimos anteriores. Sinais curtos exigem máximas mais baixas nos mesmos intervalos de tempo.
- **Confirmação do rompimento**: a estratégia espera que a melhor oferta exceda a máxima anterior (longa) ou que a melhor oferta caia abaixo da mínima anterior (curta) no período base. Além disso, os três máximos (ou mínimos) anteriores devem formar uma escada na direção do rompimento.
- **CCI janela**: o Commodity Channel Index da vela fechada anterior deve permanecer dentro de uma faixa configurável em torno de zero. Os limites positivos utilizam uma janela simétrica; limites negativos relaxam o requisito para um dos lados exatamente como no EA original.
- **Filtro de volume direcional**: os volumes do período de volatilidade são divididos em dois blocos rolantes. Uma negociação só é permitida se o bloco mais recente mostrar mais volume direcional do que o bloco mais antigo e o bloco mais antigo for diferente de zero. Valores negativos `VolatilityWindow` alternam o filtro para acumulação baseada em intervalo (não direcional).
- **Gerenciamento de riscos**:
  - Distâncias fixas de take-profit e stop-loss expressas em etapas de preço mínimo.
  - O nível de lucro é reduzido em uma etapa de preço a cada `TakeProfitReduceMinutes` minutos que a posição permanece aberta.
  - Um trailing stop é ativado após o preço ter sido movido em `TrailingStopPoints` e então segue o movimento vela por vela.
  - As posições podem ser fechadas à força após `LiveMinutes` ou no `FridayCloseHour` configurado.
  - Novas entradas são bloqueadas enquanto a posição líquida absoluta for igual a `MaxDirectionalPositions * TradeVolume` e opcionalmente enquanto o cooldown de reentrada estiver ativo.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `-5` | Tamanho do pedido. Valores positivos utilizam lotes fixos; valores negativos representam uma porcentagem do capital do portfólio convertido em volume usando o preço de venda atual. |
| `TakeProfitPoints` | `40` | Distância da entrada até a meta de lucro em etapas de preço. |
| `StopLossPoints` | `340` | Distância da entrada até o stop loss em etapas de preço. |
| `TrailingStopPoints` | `25` | Distância do trailing stop em etapas de preço. A trilha entra em ação quando o movimento ultrapassa essa distância. |
| `CciPeriod` | `14` | Período de lookback para o Commodity Channel Index calculado no período base. |
| `CciLimit` | `75` | Limite superior para entradas longas e limite negativo espelhado para entradas curtas. Valores negativos reproduzem os limites assimétricos do EA original. |
| `MaxDirectionalPositions` | `1` | Unidades máximas de posição líquida (em múltiplos do volume de negociação calculado) permitidas em uma direção. |
| `ReentryIntervalMinutes` | `0` | Número mínimo de minutos de espera entre duas entradas consecutivas. |
| `TakeProfitReduceMinutes` | `600` | Minutos antes do limite de lucro ser reduzido em uma etapa de preço. Defina como `0` para desativar a redução. |
| `LiveMinutes` | `0` | Vida útil máxima de uma posição em minutos. Um valor de `0` desativa o temporizador. |
| `VolatilityWindow` | `100` | Número de velas de volatilidade armazenadas em cada bloco rolante. Valores negativos mudam para acumulação baseada em intervalo, `0` usa apenas a vela mais recente. |
| `VolatilityThresholdPoints` | `1` | Corpo mínimo da vela (janela positiva) ou faixa (janela não direcional) necessária para acumular volume. O sinal inverte a interpretação dos volumes para cima/para baixo. |
| `FridayCloseHour` | `22` | Hora do dia (0-23) utilizada para liquidação de posições nas noites de sexta-feira. `0` desativa a saída de sexta-feira. |
| `SpreadLimitPoints` | `5.5` | Spread máximo permitido nas etapas de preço ao abrir uma nova posição. |
| `CandleType` | `1 minute` | Prazo base que gera entradas e gerencia saídas. |
| `Hour1CandleType` | `1 hour` | Período de tempo mais alto usado para confirmação da tendência H1. |
| `Hour4CandleType` | `4 hours` | Período de tempo mais alto usado para confirmação da tendência H4. |
| `Minute30CandleType` | `30 minutes` | Prazo mais alto usado para confirmação da tendência M30. |
| `VolatilityCandleType` | `1 minute` | Período que alimenta o filtro de volume direcional. |

## Notas de implementação
- A estratégia assina a carteira de pedidos para reutilizar os melhores preços de compra/venda mais recentes para detecção de rompimentos e filtragem de spread.
- Todas as vinculações de indicadores dependem do API de alto nível de StockSharp: o valor de CCI é obtido por meio de `BindEx`, enquanto prazos mais altos usam assinaturas dedicadas.
- Trailing stops e reduções de take-profit são executados em código, e não por meio de ordens de proteção, para imitar o comportamento original EA.
- Os valores negativos de `TradeVolume` dependem do preço de venda atual e das restrições de volume de segurança. Quando o tamanho calculado fica abaixo do lote mínimo, ele é automaticamente arredondado.

## Uso
1. Anexe a estratégia a um portfólio e escolha o título desejado.
2. Configure os parâmetros de prazo, limites de risco e regras de dimensionamento de volume.
3. Comece a estratégia. Os sinais são avaliados apenas em velas finalizadas; as posições são abertas com ordens de mercado e fechadas através das regras integradas de gestão de risco.
