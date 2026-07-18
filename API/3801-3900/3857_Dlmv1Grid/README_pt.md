# Estratégia de grade DLM v1.4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader 4 "DLM v1.4" de Alejandro Galindo. O robô original combina um filtro de sinal Fisher Transform com um esquema de média estilo martingale que constrói progressivamente uma grade de posições sempre que o preço se move contra a última entrada. A versão StockSharp mantém as mesmas ideias de gerenciamento de dinheiro enquanto adapta a lógica de execução e proteção ao API de alto nível (assinaturas de velas, ligações de indicadores e auxiliares de mercado/limite).

## Lógica de negociação
- Analise as velas finalizadas a partir do período de tempo configurado e calcule dois indicadores: a Transformada de Fisher e uma suavização SMA dos valores de Fisher.
- Determine a direção da cesta a partir da posição relativa das duas linhas. Quando Fisher ultrapassa o nível mais suave, a estratégia se prepara para comprar; quando cai abaixo do nível mais suave, ele se prepara para vender. O sinalizador `ReverseSignals` inverte esta interpretação.
- Abra a primeira posição imediatamente (ordem de mercado) assim que uma direção estiver disponível e a negociação automática estiver habilitada (`ManualTrading = false`).
- Enquanto a cesta estiver ativa, continue adicionando novas entradas sempre que o preço se mover `GridDistancePips` em relação à execução mais recente. Dependendo do sinalizador `UseLimitOrders`, as negociações adicionais são enviadas como ordens de mercado (no próximo fechamento da vela) ou como ordens de limite restantes posicionadas exatamente a um passo da grade do último preenchimento.
- O volume de cada nova negociação segue o crescimento original do martingale: multiplique o tamanho do lote base por 1,5 quando `MaxTrades > 12`, caso contrário, dobre o tamanho. O próprio tamanho base pode ser fixo (`LotSize`) ou derivado do patrimônio da conta quando `UseMoneyManagement` está ativado.
- Cada preenchimento atualiza os níveis agregados de stop-loss e take-profit para que toda a cesta compartilhe um único conjunto de níveis de proteção. A lógica de trailing-stop pode restringir o stop depois que o preço viaja `GridDistancePips + TrailingStopPips` na direção lucrativa.

## Proteção de conta
- **Guarda de lucros segura** (`SecureProfitProtection`): quando o número de entradas abertas atinge `OrdersToProtect`, o lucro não realizado (na moeda da conta) é comparado com `SecureProfit`. Se o limite for atingido, todo o cesto é fechado imediatamente.
- **Proteção de patrimônio** (`EquityProtection` + `EquityProtectionPercent`): monitora o valor atual do portfólio e fecha a cesta sempre que o patrimônio cair abaixo do percentual selecionado do patrimônio capturado no início da estratégia.
- **Proteção contra saque de dinheiro** (`AccountMoneyProtection` + `AccountMoneyProtectionValue`): interrompe a negociação quando o saque da moeda em relação ao patrimônio inicial excede o valor configurado.
- **Proteção vitalícia** (`OrdersLifeSeconds`): impõe um tempo de vida máximo para a entrada mais recente; quando o limite é excedido, todas as negociações são fechadas e o ciclo martingale é interrompido.
- **Filtro de sexta-feira** (`TradeOnFriday`): evita que novas cestas comecem às sextas-feiras se desativado.

Todas as saídas protetoras utilizam ordens de mercado para garantir a execução. As ordens limitadas pendentes são canceladas sempre que um bloqueio de proteção é acionado ou quando a rede é reinicializada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância de lucro compartilhada (pips) aplicada a cada entrada. |
| `StopLossPips` | Distância inicial de stop-loss (pips) para cada nova negociação. |
| `TrailingStopPips` | Distância de parada móvel que se torna ativa após o limite de disparo. |
| `MaxTrades` | Número máximo de etapas de média permitidas na cesta. |
| `GridDistancePips` | Movimento adverso mínimo (pips) antes de adicionar o próximo pedido. |
| `LotSize` | Tamanho base do lote quando o gerenciamento de dinheiro está desativado. |
| `UseMoneyManagement` | Permite dimensionamento baseado em saldo por meio da fórmula de risco original. |
| `RiskPercent` | Porcentagem de risco usada para derivar o tamanho do lote base dinâmico. |
| `AccountType` | Escala aplicada ao tamanho do lote dinâmico (0 padrão, 1 mini, 2 micro). |
| `SecureProfitProtection` | Ativa a proteção de lucro flutuante. |
| `SecureProfit` | Lucro não realizado (unidades monetárias) necessário para acionar a guarda. |
| `OrdersToProtect` | Número mínimo de entradas abertas antes da ativação do lucro seguro. |
| `EquityProtection` | Ativa a rede de segurança de percentual de patrimônio. |
| `EquityProtectionPercent` | Limite percentual de capital próprio relativo ao início da estratégia. |
| `AccountMoneyProtection` | Ativa proteção baseada em saque (moeda). |
| `AccountMoneyProtectionValue` | Rebaixamento máximo tolerado na moeda da conta. |
| `TradeOnFriday` | Permite/não permite abertura de novas cestas às sextas-feiras. |
| `OrdersLifeSeconds` | Vida útil máxima (segundos) para o último pedido antes da liquidação. |
| `ReverseSignals` | Inverte a direção da Transformada de Fisher. |
| `UseLimitOrders` | Alterne entre o mercado e limite as entradas para calcular a média das negociações. |
| `ManualTrading` | Desativa entradas automáticas quando definido como verdadeiro. |
| `CandleType` | Prazo utilizado para os cálculos do indicador. |
| `FisherLength` | Comprimento de lookback para a Transformada de Fisher. |
| `SignalSmoothing` | Período SMA aplicado para suavizar valores de Fisher. |
| `DefaultPipValue` | Valor do pip substituto usado para converter P/L não realizado em moeda. |

## Notas
- Todos os comentários no código-fonte estão em inglês, conforme exigido pelas diretrizes do repositório.
- A estratégia depende exclusivamente do StockSharp API de alto nível (`SubscribeCandles`, `Bind`, `BuyLimit`, `SellLimit`, etc.) e não manipula buffers de indicadores diretamente.
- Os cálculos de gestão de dinheiro reutilizam a fórmula de risco original, mas os ajustes de volume e preço são passados por `Security.ShrinkVolume` e `Security.ShrinkPrice` para respeitar a especificação do contrato do instrumento.
- A conversão mantém o comportamento do MetaTrader EA o mais próximo possível, ao mesmo tempo em que leva em conta as diferenças de StockSharp (por exemplo, as saídas da cesta usam ordens de mercado em vez de modificar as ordens existentes).
