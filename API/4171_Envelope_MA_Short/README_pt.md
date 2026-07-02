# Estratégia Curta Envelope MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Envelope MA Short Strategy** é uma versão C# do MetaTrader consultor especialista `EnvelopeMA.mq4` (ID 9533). Ele recria a lógica original de rompimento curto em velas de 15 minutos, combinando um envelope de média móvel exponencial com dois EMAs adicionais e um trio de filtros Parabolic SAR. A estratégia observa recuos de preço e o EMA rápida na metade inferior do envelope e, em seguida, arma uma ordem de venda pendente pendente no limite inferior do envelope. Quando a ordem é preenchida, ela gerencia a posição curta com níveis fixos de stop-loss e take-profit, bem como regras de saída baseadas em indicadores.

## Indicadores e sinais
- **Base do envelope:** Média móvel exponencial dos máximos das velas (`EnvelopePeriod`, padrão 280). A faixa inferior é o gatilho de entrada e é calculada com uma porcentagem de desvio (`EnvelopeDeviation`, padrão 0,08%).
- **EMA rápida:** Média móvel exponencial dos mínimos das velas (`FastMaPeriod`, padrão 6) usada para confirmar o impulso antes de armar a entrada curta.
- **EMA lenta (deslocado):** Média móvel exponencial dos mínimos das velas com atraso de uma barra (`SlowMaPeriod`, padrão 18). O valor atrasado reflete o parâmetro de deslocamento `iMA` de MetaTrader e é usado tanto para confirmação de entrada quanto para decisões de saída.
- **Parabolic SAR trio:** Três Parabolic SAR instâncias com diferentes fatores de aceleração (0,03/0,5, 0,015/0,6 e 0,02/0,2) que devem ficar acima do preço atual antes que a estratégia permita uma saída baseada em indicadores.

A estratégia espera pelas velas concluídas. Quando o EMA rápida, o lento deslocado EMA e o fechamento da vela permanecem entre os limites do envelope (acima da banda inferior e abaixo da banda superior), ele envia uma ordem de sell-stop na banda inferior do envelope. As ordens pendentes expiram após aproximadamente cinco intervalos de velas se permanecerem não preenchidas.

## Gestão comercial
- **Níveis de proteção:** Na entrada, a estratégia coloca metas internas de stop-loss e take-profit derivadas das distâncias de pip configuradas. Os movimentos de preços fora da faixa da vela são aproximados usando os valores máximos e mínimos de cada barra finalizada.
- **Saída do indicador:** Uma posição curta é fechada antecipadamente quando ambos os EMAs e o fechamento ficam abaixo do preço de entrada, todos os três valores SAR permanecem acima do preço e o EMA rápida cruza de volta acima do lento atrasado EMA – imitando o comportamento MetaTrader.
- **Ajuste de rastreamento:** Após pelo menos quatro barras, se a máxima mais alta da vela desde a entrada tiver se movido pelo menos três etapas de preço abaixo do preço de entrada e o fechamento estiver sendo negociado abaixo da banda inferior do envelope, o stop loss será reduzido para essa banda inferior.

## Risk controls
- **Proteção de patrimônio:** O parâmetro `LiquidityThreshold` fecha quaisquer posições vendidas abertas e cancela paradas de venda pendentes se a proporção entre o patrimônio do portfólio e o saldo inicial cair abaixo do valor configurado (padrão 0,58).
- **Expiração do pedido:** Pedidos pendentes não atendidos são cancelados automaticamente assim que o tempo de vida de cinco barras expirar para evitar sinais obsoletos.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo/prazo de vela processado pela estratégia. | Período de 15 minutos |
| `EnvelopePeriod` | Comprimento EMA usado como base do envelope. | 280 |
| `EnvelopeDeviation` | Largura do envelope expressa em porcentagem. | 0,08 |
| `FastMaPeriod` | Período EMA rápida calculado em mínimos. | 6 |
| `SlowMaPeriod` | Período EMA lento (avaliado com um atraso de uma barra). | 18 |
| `StopLossPips` | Distância de stop-loss em pips em relação ao preço de entrada. | 25 |
| `TakeProfitPips` | Distância de lucro em pips em relação ao preço de entrada. | 25 |
| `TradeVolume` | Volume utilizado para ordens pendentes e de mercado. | 1 |
| `LiquidityThreshold` | Relação mínima de capital próprio sobre saldo; shorts são liquidados quando violados. | 0,58 |

## Notas de conversão
- O dimensionamento do lote MetaTrader com base no saldo, margem ou contra-pips foi substituído por um parâmetro `TradeVolume` direto para se ajustar ao modelo de execução StockSharp.
- O carimbo de data e hora de expiração para pedidos pendentes é tratado dentro do loop de estratégia porque StockSharp pedidos não expõem o mesmo campo de vencimento que MetaTrader.
- Os níveis de stop-loss e take-profit são avaliados em relação aos máximos e mínimos das velas para aproximar os gatilhos intra-barra, correspondendo ao comportamento do especialista MQL que monitorou os preços nas barras concluídas.
