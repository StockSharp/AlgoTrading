# 4119 Estratégia Campeã
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta C# de alto nível do MetaTrader Expert Advisor localizado em `MQL/919/champion.mq5`. O EA original aguarda um sinal do Índice de Força Relativa (RSI) e coloca três ordens de parada na direção do rompimento previsto. Cada ordem pendente já inclui um stop-loss e um take-profit e o stop-loss é seguido sempre que o preço se move favoravelmente. A versão StockSharp mantém o mesmo comportamento enquanto depende exclusivamente de chamadas API de alto nível (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, etc.).

A configuração padrão tem como alvo instrumentos FX líquidos onde o "ponto" MetaTrader corresponde ao StockSharp `PriceStep` (normalmente 0,0001). O tipo de vela é configurável e a estratégia pode ser aplicada a qualquer período de tempo, desde que a conta forneça as melhores cotações de compra/venda e, opcionalmente, informações de nível de stop.

## Lógica estratégica
1. **Geração de sinal**
   - Um RSI de comprimento configurável é calculado em velas concluídas.
   - O valor RSI anterior (uma barra fechada atrás) é comparado com um limite simétrico (`RsiLevel`).
   - `RSI < RsiLevel` desencadeia uma configuração de alta; `RSI > 100 - RsiLevel` desencadeia uma configuração de baixa.
2. **Colocação de pedido pendente**
   - Quando não há posições abertas nem ordens pendentes ativas gerenciadas pela estratégia, três ordens stop idênticas são colocadas na direção sinalizada.
   - As paradas de compra são colocadas acima da melhor oferta e as paradas de venda abaixo da melhor oferta. A distância respeita o nível de parada fornecido pelo servidor (se disponível) ou o substituto `MinOrderDistancePoints`.
   - O volume do pedido é calculado dinamicamente: valor da conta disponível dividido por `BalancePerLot`, limitado ao intervalo do lote `[0.1, 15]` e arredondado para duas casas decimais. Cada ordem pendente recebe um terço do volume computado.
3. **Ordens de proteção iniciais**
   - Assim que a primeira negociação é preenchida, as ordens de proteção agregadas são registradas: stop-loss em `entry ± StopLossPoints` e take-profit em `entry ± TakeProfitPoints` (MetaTrader pontos convertidos em preço por `PriceStep`).
   - Se `TakeProfitPoints` for zero, a ordem de realização de lucro será desativada.
4. **Parada final**
   - Enquanto uma posição está aberta, a ordem de stop loss é reforçada em cada atualização de nível 1.
   - Para posições compradas, o novo stop é igual a `max(entry + spread, bid - StopLoss)`; para shorts `min(entry - spread, ask + StopLoss)`.
   - O trailing é ativado somente quando o movimento excede a soma do nível de stop da corretora e do spread atual, reproduzindo as salvaguardas EA originais.
5. **Manutenção de pedido pendente**
   - As paradas de compra pendentes são movidas para mais perto do mercado quando seu preço de ativação está a mais de `RepriceDistancePoints` de distância da oferta atual. A mesma lógica se aplica aos stops de venda versus o lance atual.
   - A reprecificação sempre respeita o maior entre `RepriceDistancePoints` e a distância efetiva do nível de parada.
6. **Posição de saída**
   - As posições fecham através de ordens protetoras de stop-loss/take-profit ou por intervenção manual do usuário. Quando o tamanho da posição retorna a zero, a estratégia cancela quaisquer ordens de proteção restantes e aguarda o próximo sinal RSI.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPoints` | MetaTrader pontos adicionados/subtraídos do preço de preenchimento para colocar o pedido de lucro. Defina como `0` para desativar o alvo. |
| `StopLossPoints` | MetaTrader pontos adicionados/subtraídos do preço de preenchimento para colocar a ordem de stop loss e calcular a distância final. |
| `RsiPeriod` | RSI comprimento (número de velas). |
| `RsiLevel` | Limite simétrico RSI. Valores abaixo do nível acionam posições compradas, valores acima de `100 - level` acionam posições vendidas. |
| `BalancePerLot` | Valor da moeda da conta considerado equivalente a um lote padrão ao dimensionar posições. |
| `MinOrderDistancePoints` | Distância mínima de fallback (em pontos) entre o preço de mercado e as novas ordens de stop quando a plataforma de negociação não reporta um nível de stop. |
| `RepriceDistancePoints` | Distância (em pontos) que aciona a reprecificação de ordens pendentes. |
| `CandleType` | Tipo de dados Candle usado para o cálculo RSI. |

## Notas de uso
- A estratégia requer dados de velas e cotações de nível 1 (melhor oferta/venda). Sem atualizações de nível 1, a lógica de rastreamento e a manutenção de ordens pendentes são desabilitadas.
- Quando o corretor expõe um nível de parada ou distância de parada por meio de metadados de nível 1, ele é automaticamente respeitado. Caso contrário, configure `MinOrderDistancePoints` para atender aos requisitos do instrumento.
- O dimensionamento da posição volta para a propriedade `Strategy.Volume` sempre que faltam informações do portfólio ou o tamanho do lote calculado se torna não positivo.
- Três ordens pendentes são sempre colocadas juntas. Cancelar manualmente pedidos indesejados caso seja necessária participação parcial; a estratégia continuará a gerir os restantes.

## Gestão de risco
- As ordens stop-loss e take-profit são ordens nativas de bolsa/corretora, refletindo o comportamento do MetaTrader EA. Quando uma posição é fechada, as ordens de proteção são canceladas imediatamente.
- O trailing stop apenas se move na direção do lucro e nunca afrouxa o stop loss. Ele é ativado quando o preço ultrapassa pelo menos `(StopLevel + spread)` o preço de entrada.
- A lógica de reprecificação evita que ordens pendentes obsoletas sejam deixadas para trás após grandes saltos, reduzindo a probabilidade de atrasos no preenchimento.
