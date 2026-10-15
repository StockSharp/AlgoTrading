# Duas estratégias de filtro intradiário EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o MetaTrader Expert Advisor **Expert_2EMA_ITF** usando o StockSharp API de alto nível. Ele negocia no cruzamento de duas médias móveis exponenciais e usa o intervalo verdadeiro médio (ATR) para definir ordens de limite pendentes, paradas de proteção e metas. Um filtro de horário intradiário adicional bloqueia entradas durante minutos, horas ou dias da semana indesejados.

## Resumo da lógica
- Calcule valores EMA rápidos e lentos na série de velas selecionada.
- Detecte um cruzamento de alta quando o rápido EMA subir acima do lento EMA e um cruzamento de baixa quando cair abaixo.
- Em um cruzamento de alta, coloque uma ordem de limite de compra compensada do lento EMA em `LimitMultiplier * ATR` mais o spread atual. Em um cruzamento de baixa, coloque uma ordem de limite de venda compensada na direção oposta.
- Armazene preços de stop-loss e take-profit usando multiplicadores ATR para que possam ser enviados imediatamente assim que o pedido de entrada for preenchido.
- Cancele pedidos pendentes automaticamente se eles permanecerem não atendidos por mais de `ExpirationBars` velas.
- Ignorar sinais que não passam no filtro intradiário (permitidas verificações de minutos, horas e dias). As máscaras de bits podem desativar vários minutos, horas ou dias simultaneamente.

## Indicadores
- **Rápido EMA** – controla a sensibilidade da detecção de cruzamento.
- **Lento EMA** – define a direção da tendência.
- **Average True Range (ATR)** – mede a volatilidade do mercado e dimensiona as compensações de preços de entrada/saída.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período usado para cálculos. | Velas de 30 minutos |
| `FastEmaPeriod` | Período do jejum EMA. | 5 |
| `SlowEmaPeriod` | Período do lento EMA (deve ser maior que o período rápido). | 30 |
| `AtrPeriod` | ATR período de cálculo. | 7 |
| `LimitMultiplier` | Multiplicador ATR que altera os preços de entrada limitados. | 1.2 |
| `StopLossMultiplier` | Multiplicador ATR para colocação de stop-loss. | 5 |
| `TakeProfitMultiplier` | Multiplicador de ATR para colocação com fins lucrativos. | 8 |
| `ExpirationBars` | Número de barras após as quais os pedidos não atendidos são cancelados. | 4 |
| `GoodMinuteOfHour` | Minuto específico permitido para entradas (-1 desabilita). | -1 |
| `BadMinutesMask` | Minutos de bloqueio de máscara de bits (bit *n* bloqueia minutos *n*). | 0 |
| `GoodHourOfDay` | Hora específica permitida para entradas (-1 desabilita). | -1 |
| `BadHoursMask` | Horas de bloqueio de máscara de bits (bit *n* bloqueia horas *n*). | 0 |
| `GoodDayOfWeek` | Dia específico permitido para entradas (-1 desabilita, 0 = domingo). | -1 |
| `BadDaysMask` | Dias de bloqueio de máscara de bits (bit *n* bloqueia dia *n*, 0 = domingo). | 0 |

## Gerenciamento de ordens
1. **Ordens de entrada** – As ordens limitadas são registradas com um preço deslocado do lento EMA pela compensação baseada em ATR. A ordem de compra também adiciona o spread atual se as cotações de compra/venda estiverem disponíveis.
2. **Expiração** – Cada ordem pendente armazena o índice da vela quando foi criada. Se `ExpirationBars` for positivo e a ordem sobreviver além de tantas barras, ela será cancelada automaticamente.
3. **Ordens de proteção** – Quando uma ordem de entrada é preenchida, a estratégia cancela quaisquer ordens stop/target anteriores e, em seguida, coloca imediatamente um stop-loss e um take-profit calculados a partir do instantâneo ATR que gerou o sinal. As ordens de proteção opostas são canceladas quando a posição é estável.

## Detalhes do filtro intradiário
- **Valores permitidos únicos** – `GoodMinuteOfHour`, `GoodHourOfDay` e `GoodDayOfWeek` restringem a negociação a um minuto, hora ou dia da semana específico quando não são negativos.
- **Máscaras de bits** – `BadMinutesMask`, `BadHoursMask` e `BadDaysMask` contêm bits que desativam vários intervalos de tempo ao mesmo tempo. Por exemplo, a configuração `BadMinutesMask = (1 << 0) | (1 << 30)` bloqueia a negociação durante o minuto 0 e o minuto 30 de cada hora.
- **Lógica combinada** – Uma entrada só é permitida quando o tempo atual da vela ultrapassa todas as condições permitidas e nenhuma das máscaras a bloqueia.

## Diferenças em relação ao Expert Advisor original
- A versão StockSharp usa ordens de limite pendentes combinadas com registros explícitos de stop-loss e take-profit assim que a entrada é executada, refletindo os cálculos do sinal MQL.
- A compensação de spread para ordens de compra usa as cotações atuais de `Security.BestBid/BestAsk` quando estão disponíveis, caso contrário, o deslocamento é zero.
- A filtragem de tempo é expressa por meio de máscaras de bits e comparações diretas em vez de MetaTrader classes auxiliares de filtro de tempo específicas.
- Todas as ações de negociação utilizam StockSharp auxiliares de alto nível (`BuyLimit`, `SellLimit`, `SellStop`, `BuyStop`) e lógica de cancelamento automático em vez de matrizes de pedidos manuais.

## Notas de uso
- Certifique-se de que o volume da estratégia esteja definido antes de iniciar a estratégia; caso contrário, será gerado um aviso e nenhum pedido será enviado.
- Para cenários de otimização, os metadados do parâmetro já permitem o ajuste de EMA períodos, ATR período, multiplicadores e duração de expiração.
- A estratégia assume que os tempos de fechamento das velas representam o fim da barra e os utiliza ao avaliar os filtros intradiários.
