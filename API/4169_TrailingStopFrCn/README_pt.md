# Estratégia TrailingStopFrCn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`TrailingStopFrCnStrategy` é uma porta StockSharp do consultor especialista MetaTrader **TrailingStopFrCn.mq4**. O script original gerencia níveis de stop-loss para posições existentes usando uma combinação de distâncias finais fixas, fractais Bill Williams ou máximos/mínimos recentes de velas. Esta porta mantém a mesma flexibilidade enquanto se integra ao StockSharp API de alto nível: a estratégia assina velas e cotações de nível 1, monitora a posição líquida atual e atualiza automaticamente uma ordem de stop protetora.

Ao contrário de uma estratégia de entrada, o TrailingStopFrCn concentra-se exclusivamente na gestão de risco. Não abre novas posições. Em vez disso, ele rastreia a posição existente de `Strategy.Security`, cancela ordens de stop obsoletas quando a posição muda e envia uma única ordem de stop agregada que segue a lógica do consultor MetaTrader.

## Lógica final

1. **Distância de fuga fixa** – quando `TrailingStopPips` é maior que zero, a estratégia se comporta como o parâmetro MQL original `TrailingStop`. Para posições longas o stop é colocado em `bestBid - distance`, para posições curtas em `bestAsk + distance`, com `distance = TrailingStopPips × pip size`.
2. **Fractal trailing** – quando `TrailingStopPips = 0` e `TrailingMode = Fractals`, a estratégia detecta fractais Bill Williams de cinco barras. Cada vela finalizada é adicionada a um buffer interno e, uma vez disponível histórico suficiente, a vela duas barras atrás é avaliada como um fractal potencial. O fractal mais recente que está pelo menos `MinStopDistancePips` longe do preço atual torna-se o novo candidato a stop.
3. **Trailing de vela** – quando `TrailingStopPips = 0` e `TrailingMode = Candles`, a estratégia verifica até as últimas 99 velas fechadas e seleciona a primeira mínima (para posições compradas) ou máxima (para posições vendidas) que está separada do preço atual por pelo menos `MinStopDistancePips`.

Depois de calcular o nível de candidato, a estratégia impõe as mesmas regras de proteção da versão MQL:

- **OnlyProfit** evita mover o stop, a menos que o novo nível bloqueie o lucro (stop acima da entrada para posições compradas, stop abaixo da entrada para posições vendidas).
- **OnlyWithoutLoss** interrompe o trailing quando o stop-loss ativo já protege a posição contra perdas (no script original, o processo de trailing para após o ponto de equilíbrio ser atingido).
- O stop só se move na direção favorável: para cima para posições longas e para baixo para posições curtas.

Como StockSharp rastreia uma única posição líquida por título, o volume da ordem stop é igual a `Math.Abs(Position)` e todos os preenchimentos subjacentes são agregados.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OnlyProfit` | Mova o stop-loss somente quando o novo nível garantir lucro em relação ao preço médio de entrada. Espelha o sinalizador `OnlyProfit` de MQL. |
| `OnlyWithoutLoss` | Pare de seguir quando o stop loss ativo estiver igual ou superior ao preço de entrada. Isso replica `OnlyWithoutLoss` do orientador original. |
| `TrailingStopPips` | Distância de fuga fixa expressa em pips. Defina como zero para ativar o fractal ou o rastro de vela. |
| `MinStopDistancePips` | Distância mínima (em pips) entre o preço de mercado e o stop loss. Use-o para emular a restrição do corretor `MODE_STOPLEVEL`. |
| `TrailingMode` | Escolhe a origem final quando `TrailingStopPips = 0`. Opções: `Fractals` (Bill Williams fractais de cinco barras) ou `Candles` (mínimos/máximos recentes). |
| `CandleType` | Tipo de dados Candle usado para construir fractais ou para procurar pontos de oscilação. O padrão é o período de uma hora. |

## Notas comportamentais

- A estratégia subscreve dados de Nível 1 para acessar os melhores preços de compra/venda. O rastreamento de distância fixa reage imediatamente às atualizações de nível 1, enquanto o rastreamento fractal/vela é atualizado quando novas velas chegam.
- Quando a direção da posição muda, a ordem de parada atual é cancelada antes que a nova ordem seja enviada.
- Se nenhum candidato a stop estiver disponível (por exemplo, velas insuficientes), a estratégia mantém o stop existente.
- Se o corretor não impor uma distância mínima de parada, você poderá deixar `MinStopDistancePips` em zero.

## Diferenças da versão MetaTrader

- StockSharp mantém uma posição líquida, portanto, “tickets” individuais de MetaTrader não são rastreados. A ordem stop cobre toda a posição agregada.
- O filtro `Magic` não é necessário: a estratégia já opera em seu próprio contexto de segurança.
- As atualizações finais são conduzidas por velas concluídas mais dados de Nível 1, em vez de um loop de pesquisa de um segundo.
- Objetos gráficos visuais do EA original não são recriados; em vez disso, você pode usar os auxiliares de gráficos de StockSharp ao executar a IU de amostra.

## Dicas de uso

1. Execute a estratégia junto com qualquer lógica de entrada que abra posições no mesmo `Security`. TrailingStopFrCn anexará automaticamente uma ordem de stop assim que a posição aparecer.
2. Ajuste `CandleType` para corresponder ao período de tempo que deve ser analisado para fractais ou pontos de oscilação. Prazos mais altos suavizam os níveis finais, enquanto prazos mais baixos reagem mais rapidamente.
3. Calibre `MinStopDistancePips` de acordo com as limitações de nível de stop da sua corretora. Definir um valor muito baixo pode levar à rejeição de pedidos.
4. Ao testar dados históricos, certifique-se de que a assinatura de vela e as mensagens de nível 1 estejam disponíveis na fonte de dados para que a lógica de rastreamento possa ser acionada corretamente.
