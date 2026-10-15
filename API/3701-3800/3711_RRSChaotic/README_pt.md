# Estratégia Caótica RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **RRS Chaotic EA** original lança continuamente os dados em cada tick, escolhendo símbolos aleatórios e tamanhos de posição antes de enviar ordens de mercado. A porta StockSharp mantém o espírito do caos controlado, direcionando entradas de um fluxo de vela na segurança configurada. Cada vela fechada desencadeia uma nova decisão aleatória tanto para direção quanto para volume, ao mesmo tempo que reflete as regras de gestão de dinheiro do consultor especialista.

## Principais recursos
- **Entradas aleatórias** – cada vela finalizada gera um número inteiro aleatório de 0 a 10. Os valores `6` ou `9` abrem uma posição longa, enquanto `3` ou `8` abrem uma posição curta, correspondendo à lógica MT4.
- **Volume variável** – o volume negociado é amostrado uniformemente entre os parâmetros *MinVolume* e *MaxVolume* e alinhado com a etapa de volume do título.
- **Filtro de spread** – novas posições são bloqueadas sempre que o spread atual (em pontos) exceder *MaxSpreadPoints*.
- **Take-profit e stop-loss** – saídas opcionais baseadas em pontos que recriam as configurações de nível de pedido do especialista.
- **Proteção contra saques** – as perdas não realizadas são continuamente comparadas com um limite de caixa fixo ou com uma porcentagem do valor do portfólio. A violação do limite cancela as ordens ativas e nivela a posição.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `CandleType` | Série de velas usadas para acionar a estratégia (velas padrão de 1 minuto). |
| `MinVolume` / `MaxVolume` | Faixa para geração de lote aleatório. |
| `TakeProfitPoints` | Distância de lucro em faixas de preço. Defina como `0` para desativar. |
| `StopLossPoints` | Distância de stop-loss em faixas de preço. Defina como `0` para desativar. |
| `MaxOpenTrades` | Volume líquido máximo medido em etapas de volume que podem permanecer abertas simultaneamente. |
| `MaxSpreadPoints` | Spread máximo permitido, expresso em faixas de preço. |
| `SlippagePoints` | Parâmetro de deslizamento informativo (mantido para fins de integridade). |
| `RiskControlMode` | Seleciona entre modelos de risco `FixedMoney` e `BalancePercentage`. |
| `RiskValue` | Ou a quantidade de dinheiro a arriscar ou a percentagem de capital próprio, dependendo da modalidade. |
| `TradeComment` | Tag anexada aos pedidos gerados para facilitar a auditoria. |

## Lógica da estratégia
1. Assine a série de velas configuradas e aguarde as velas finalizadas.
2. Aplicar controle de rebaixamento. Se a perda não realizada ultrapassar o limite, cancele as ordens ativas e feche a posição atual.
3. Mantenha metas opcionais de stop-loss e take-profit que refletem as configurações do pedido MT4.
4. Quando a negociação for permitida e o spread for aceitável, role um número aleatório para decidir se abre uma posição longa ou curta.
5. Limite a exposição acumulada limitando o número de etapas de volume a `MaxOpenTrades`.

## Diferenças vs. versão MQL4
- O especialista original negociou vários símbolos aleatórios. As estratégias StockSharp operam em um único título; portanto, a aleatoriedade é aplicada apenas à direção e ao tamanho.
- As paradas de proteção são executadas por meio de ordens de mercado no fechamento de velas, em vez de parâmetros nativos de ordens de stop-loss/take-profit.
- A avaliação de spread usa o melhor lance/venda atual em vez da função MT4 `MarketInfo`.
- Todas as ordens geradas incluem o texto *TradeComment*, fornecendo um contexto semelhante aos números mágicos do MT4.

## Notas de uso
- Certifique-se de que a segurança conectada exponha valores `PriceStep`, `MinStep` e `VolumeStep` válidos para uma conversão precisa do ponto em preço.
- O período de vela padrão é de um minuto para emular a aleatoriedade no nível do tick sem sobrecarregar o pipeline de backtesting. Aumente o prazo para reduzir a frequência de negociação.
- O controle de risco depende do lucro não realizado derivado da posição agregada. Cestas mistas longas/curtas, como vistas na versão MT4, não são suportadas por StockSharp e, portanto, são compensadas.
