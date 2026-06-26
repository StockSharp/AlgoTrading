# Estratégia Exp Hans Indicator Sistema de Nuvem Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp Hans Indicator Sistema de Nuvem Tm Plus é uma estratégia de rompimento baseada em sessões que reproduz o comportamento do expert advisor MQL5 original. O algoritmo monitora os estados de cor produzidos pelo indicador Hans em um período configurável. Abre uma nova posição depois que um rompimento de alta (cores 0/1) ou de baixa (cores 3/4) termina e o preço retorna dentro do canal. A implementação mantém todas as decisões de trading em velas fechadas, usa limites de risco baseados em pips, e espelha a regra de liquidação baseada em tempo da versão MQL.

A estratégia opera em um único par instrumento/feed de velas obtido de `GetWorkingSecurities()`. Todos os tamanhos de ordem são derivados da propriedade `Volume` da estratégia e da fração de gestão de dinheiro exposta pelos parâmetros.

## Lógica do indicador
1. Os timestamps das velas são convertidos do tempo do broker (`LocalTimeZone`) para o fuso horário alvo (`DestinationTimeZone`). Por padrão o script trabalha com GMT+4, que corresponde à implementação de referência.
2. São coletados dois intervalos de sessão de Londres a cada dia de trading:
   - **Intervalo 1**: 04:00–08:00 hora alvo. O máximo/mínimo deste período torna-se o canal de rompimento inicial.
   - **Intervalo 2**: 08:00–12:00 hora alvo. Uma vez completado, substitui o primeiro intervalo pelo resto do dia.
3. Cada intervalo é estendido por `PipsForEntry` pips em ambos os lados. Um pip é igual ao `PriceStep` do instrumento, multiplicado por 10 quando o ativo tem 3 ou 5 casas decimais (pips fracionários estilo MetaTrader).
4. As cores das velas são derivadas exatamente como no indicador:
   - Fechamento acima da faixa superior → cor `0` (fechamento de alta) ou `1` (fechamento de baixa).
   - Fechamento abaixo da faixa inferior → cor `4` (fechamento de baixa) ou `3` (fechamento de alta).
   - Fechamento dentro do canal → cor neutra `2`.

## Regras de trading
- **Entrada**: Quando a vela fechada anterior teve uma cor de alta (0/1) e a mais recente não é de alta, a estratégia abre uma posição comprada (se habilitada). Simetricamente, uma cor de baixa anterior (3/4) seguida de uma cor neutra/contrária aciona uma entrada vendida.
- **Saída**:
  - Saída direcional quando a cor anterior se volta contra a posição atual (0/1 para vendidas, 3/4 para compradas).
  - Saída opcional baseada em tempo quando o período de manutenção excede `HoldingMinutes`.
  - Níveis opcionais de stop-loss / take-profit expressos em pontos (`StopLossPoints`, `TakeProfitPoints`). Os níveis são ignorados se o ativo não expõe um `PriceStep` positivo.
- As saídas são processadas antes das novas entradas, portanto uma posição é nivelada antes que uma ordem de reversão seja enviada.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `MoneyManagement` | Fração do `Volume` da estratégia usada por operação. Valores ≤ 0 recorrem ao volume completo. | `0.1` |
| `MoneyMode` | Marcador de posição para os modos de gestão de dinheiro originais. Atualmente apenas `Lot` é aplicado. | `Lot` |
| `StopLossPoints` / `TakeProfitPoints` | Stop protetor e alvo de lucro expressos em pontos (pips). Definir como `0` para desabilitar. | `1000` / `2000` |
| `DeviationPoints` | Desvio máximo de execução aceitável em pontos. Presente para compatibilidade; não aplicado pela camada de ordens do StockSharp. | `10` |
| `AllowBuyEntries` / `AllowSellEntries` | Habilita entradas compradas/vendidas. | `true` |
| `AllowBuyExits` / `AllowSellExits` | Habilita saídas automatizadas para posições compradas/vendidas. | `true` |
| `UseTimeExit` | Ativa o filtro de liquidação baseado em tempo. | `true` |
| `HoldingMinutes` | Tempo máximo de manutenção para qualquer posição em minutos. | `1500` |
| `PipsForEntry` | Offset de pip adicionado acima/abaixo dos intervalos de rompimento. | `100` |
| `SignalBar` | Offset de vela fechada usado para sinais. Use valores ≥ 1 para se manter alinhado com a lógica MT5. | `1` |
| `LocalTimeZone` | Fuso horário do broker/servidor (horas a partir do UTC). | `0` |
| `DestinationTimeZone` | Fuso horário alvo usado para os limites de sessão. | `4` |
| `CandleType` | Período usado para os cálculos de Hans. | Velas de `30m` |

## Gestão de dinheiro e execução
- Tamanho da ordem = `Volume * MoneyManagement`, normalizado ao `VolumeStep` do instrumento. Se o valor calculado for não positivo, a lógica recorre a um passo de volume.
- Quando um sinal de reversão aparece, a estratégia envia uma única ordem de mercado igual ao novo volume mais qualquer quantidade oposta aberta. Isso reproduz o comportamento de `BuyPositionOpen`/`SellPositionOpen` do helper MQL.
- Os níveis de stop-loss e take-profit são recalculados a cada entrada e limpos quando uma posição é fechada ou revertida.

## Diretrizes de uso
1. Anexe a estratégia a um ativo que publique metadados válidos de `PriceStep`, `Decimals` e `VolumeStep`.
2. Defina o `Volume` desejado na estratégia antes de iniciá-la. A fração de gestão de dinheiro será aplicada em cima disso.
3. Escolha um tipo de vela igual ao usado no MetaTrader (M30 por padrão). Todos os cálculos dependem de velas completadas.
4. Alinhe os fusos horários se a sua fonte de dados de mercado diferir do tempo alvo GMT+4 padrão usado pelo indicador Hans.
5. Monitore os logs para mensagens sobre tamanho de pip ausente; os níveis de risco serão ignorados quando nenhum `PriceStep` estiver disponível.

## Notas de implementação
- A detecção de cor é realizada exclusivamente em velas finalizadas através da API de alto nível `SubscribeCandles`, evitando buffers de indicadores manuais.
- Os níveis de rompimento são recomputados uma vez por vela e armazenados em cache na memória; nenhuma coleção histórica é criada.
- `DeviationPoints` é mantido para completidade de configuração, mas não pode ser aplicado com ordens de mercado simples no StockSharp.
- A estratégia redefine seu estado interno em `OnReseted()` para suportar backtests repetidos sem dados de sessão obsoletos.

## Limitações
- A implementação atual só suporta `SignalBar ≥ 1`, correspondendo ao comportamento original do EA em eventos de nova barra. Usar `0` exigiria acesso ao nível de tick que não está presente no port de alto nível.
- Modos de gestão de dinheiro além de `Lot` não estão implementados. Estenda `GetOrderVolume()` se seu fluxo de trabalho depende de dimensionamento baseado em saldo.
- Sem um valor válido de `PriceStep`, as distâncias baseadas em pips (stop, take-profit, offsets de Hans) não podem ser calculadas e serão ignoradas.
