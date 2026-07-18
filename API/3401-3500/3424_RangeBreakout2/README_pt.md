# Estratégia RangeBreakout2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia RangeBreakout2** é uma versão StockSharp do consultor especialista MetaTrader "RangeBreakout2". O algoritmo prepara uma faixa de preço em horários configuráveis ​​(semanalmente, diariamente ou continuamente) e abre uma única ordem de mercado assim que as cotações de compra/venda escapam dessa faixa. Após cada negociação, o ciclo de preparação da faixa é reiniciado. A implementação reproduz as regras originais de gerenciamento de dinheiro (escala constante, linear, martingale e Fibonacci) e a expansão opcional da distância de obtenção de lucro após uma negociação perdedora.

A estratégia funciona com um único título e conta com as melhores cotações bid/ask. Certifique-se de que o adaptador forneça dados atualizados do livro de pedidos para que a detecção de fugas permaneça responsiva.

## Lógica de negociação

1. **Agendamento** – No momento configurado, a estratégia registra o preço de venda atual como o centro da configuração e deriva os níveis de rompimento superior/inferior da faixa bruta.
2. **Cálculo de faixa** – A faixa bruta é obtida de um dos três modos:
   - **ATR** – Multiplica o valor médio do intervalo verdadeiro mais recente por `AtrPercentage`.
   - **Porcentagem** – Usa `PricePercentage` por cento do preço de venda atual.
   - **Fixo** – Converte `FixedRangePoints` passos de preço em uma distância absoluta.
3. **Detecção de breakout** – Durante a fase `Setup`, a estratégia observa o melhor lance/venda. Quando o pedido se move acima do nível superior ou o lance cai abaixo do nível inferior, ele envia uma ordem de mercado.
4. **Tipo de entrada** – `TradeMode` seleciona entre breakout (`Stop`), fade (`Limit`) ou comportamento aleatório. O modo aleatório escolhe breakout ou fade em cada entrada.
5. **Proteção** – As compensações de stop-loss e take-profit são derivadas da faixa bruta. Se a negociação anterior fechou com perda e `RangeMultiplier` for maior que 1, a distância de realização do lucro será expandida por esse multiplicador.
6. **Gerenciamento de dinheiro** – O volume do pedido é calculado a partir do capital livre do portfólio (`CurrentValue - BlockedValue`) e do modo de lote selecionado:
   - **Constante** – Sempre usa o volume base.
   - **Linear** – Aumenta linearmente após cada perda.
   - **Martingale** – Multiplica o volume anterior por `LotMultiplier` após uma perda.
   - **Fibonacci** – Cresce seguindo a sequência Fibonacci após perdas.

Assim que a posição for fechada, a estratégia volta à fase de espera e aguarda o próximo acionamento do cronograma.

## Parâmetros

| Grupo | Nome | Descrição | Padrão |
|-------|------|-------------|---------|
| Cronograma | `Periodicity` | Frequência de preparação da faixa: Semanal, Diário ou Contínuo. | `Weekly` |
| Cronograma | `Day` | Dia de negociação usado quando `Periodicity` = Semanal. | `Monday` |
| Cronograma | `Hour` | Hora do dia em que a configuração é criada (ajuste estilo MetaTrader: valor armazenado + 1, limitado a 0 se ≥ 23). | `0` |
| Alcance | `RangeMode` | Método de cálculo de intervalo bruto (ATR / Porcentagem / Fixo). | `Atr` |
| Alcance | `AtrPercentage` | Multiplicador percentual aplicado ao valor ATR. | `50` |
| Alcance | `AtrLength` | Número de velas usadas no indicador ATR. | `20` |
| Alcance | `PricePercentage` | Porcentagem do preço de venda atual usado quando `RangeMode = Percent`. | `1` |
| Alcance | `FixedRangePoints` | Faixa fixa expressa em etapas de preço quando `RangeMode = Fixed`. | `1000` |
| Negociação | `RangePercentage` | Porcentagem do intervalo bruto aplicada aos níveis de ruptura. | `100` |
| Negociação | `TradeMode` | Estilo de entrada: Stop (breakout), Limit (fade) ou Random. | `Stop` |
| Negociação | `TakeProfitPercentage` | Distância de lucro como porcentagem do intervalo (opcionalmente expandido). | `100` |
| Negociação | `StopLossPercentage` | Distância de stop loss como porcentagem do intervalo base. | `100` |
| Risco | `LotMode` | Esquema de gerenciamento de lote (Constante / Linear / Martingale / Fibonacci). | `Martingale` |
| Risco | `MarginPercentage` | Parcela do capital livre reservada ao volume base do pedido. | `10` |
| Risco | `LotMultiplier` | Multiplicador aplicado em modos de escala tipo martingale. | `2` |
| Risco | `RangeMultiplier` | Multiplicador de lucro aplicado após uma negociação perdida. | `1` |
| Dados | `SignalCandleType` | Tipo de vela utilizado para verificar as condições de agendamento. | `1m time-frame` |
| Dados | `AtrCandleType` | Tipo de vela usado para cálculo de ATR. Solicitado apenas quando `RangeMode = Atr`. | `1d time-frame` |

## Notas de implementação

- A estratégia requer atualizações de compra/venda em tempo real; sem eles, a detecção de fuga não será acionada.
- Os cálculos de volume base baseiam-se no patrimônio do portfólio (`CurrentValue - BlockedValue`). Quando o conector não alimenta esses campos, o volume volta ao mínimo de troca.
- Ordens de proteção são feitas por meio de `SetStopLoss` e `SetTakeProfit`. A posição resultante (após a nova negociação) é passada para que a classe base possa gerenciar a proteção combinada para cenários de escalabilidade.
- O substituto ATR imita o consultor especialista original: se o indicador não estiver pronto, o intervalo padrão é 1% do preço de venda atual.
- O modo de negociação aleatória usa a classe .NET `Random` propagada na construção da estratégia. Dois rompimentos consecutivos podem, portanto, escolher diferentes tipos de entrada.

## Dicas de uso

1. Configure o `SignalCandleType` para corresponder à resolução desejada das verificações de agendamento. Um fluxo de velas de um minuto reproduz de perto o comportamento orientado por ticks da versão MQL.
2. Para programações semanais, certifique-se de que o fuso horário do servidor corresponda à expectativa do EA original.
3. Monitore o efeito de `RangeMultiplier` ao usar modos de lote do tipo martingale: aumentar a distância de lucro junto com volumes crescentes aumenta a exposição após sequências de perdas.
4. Como as distâncias de stop-loss e take-profit são derivadas da faixa bruta, valores grandes de `RangePercentage` levam a compensações de proteção igualmente grandes.
