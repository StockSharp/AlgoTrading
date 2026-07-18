# Estratégia LazyBot V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

LazyBot V1 é uma estratégia de breakout diária convertida do consultor especialista MetaTrader 5 original. Todos os dias de negociação, ele coloca um par de ordens de stop pendentes em torno da faixa de preço do dia anterior e usa um trailing stop para proteger as posições abertas. A conversão aproveita o StockSharp API de alto nível com assinaturas de velas e gerenciamento automático de pedidos.

## Lógica de negociação

1. Aguarde a conclusão de uma vela do período configurado (diariamente por padrão).
2. Em um novo dia, opcionalmente, certifique-se de que o horário atual do servidor esteja dentro da janela de negociação permitida e pule os finais de semana.
3. Cancele quaisquer ordens pendentes de breakout existentes criadas pela estratégia.
4. Coloque um stop de compra acima da máxima do dia anterior e um stop de venda abaixo da mínima do dia anterior. O parâmetro `Breakout Offset (pips)` adiciona distância extra a ambos os níveis de ruptura.
5. Quando qualquer uma das ordens for acionada, mantenha o stop-loss de proteção a uma distância fixa e acompanhe-o sempre que o preço avançar a favor da negociação mais do que a distância configurada do pip.
6. Recalcule o volume para os próximos pedidos usando um tamanho de lote fixo ou o módulo de dimensionamento baseado em risco.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| Tipo de vela | Prazo utilizado para coletar as velas de referência (diariamente por padrão). |
| Nome do bot | Valor escrito nos comentários do pedido para facilitar o rastreamento. |
| Stop Loss (pips) | Distância usada para o stop inicial e final. |
| Deslocamento de ruptura (pips) | Distância extra aplicada à máxima/mínima anterior ao colocar as ordens pendentes. |
| Spread máximo (pips) | Spread máximo permitido antes de criar novas ordens de breakout. Defina como 0 para desativar a verificação. |
| Use o horário de negociação | Ativa o filtro de hora de início semelhante ao EA original. |
| Hora de início | Primeira hora (inclusive) em que novos pedidos poderão ser feitos. |
| Hora final | Hora em que novos pedidos deixam de ser agendados. Quando igual à hora de início, o filtro atua como um limite inferior simples. |
| % de risco de uso | Permite cálculo de volume baseado em risco. |
| % de risco | Porcentagem do patrimônio do portfólio usado para dimensionar posições quando `Use Risk %` está ativado. |
| Volume Fixo | Volume de ordem fixo usado quando o dimensionamento de risco está desabilitado. Quando zero, a estratégia volta para a propriedade global `Volume` (o padrão é 0,01). |

## Gestão de risco

* O trailing stop reflete a lógica de trailing MetaTrader, mantendo o stop loss `Stop Loss (pips)` longe do melhor bid/ask e apenas apertando quando um preço melhor é alcançado.
* O filtro de spread protege a estratégia de enviar novas ordens de breakout quando o mercado estiver muito amplo.
* O dimensionamento baseado em risco divide o risco monetário permitido (`equity * Risk %`) pela distância de stop expressa em unidades de preço e nunca fica abaixo do tamanho de lote fixo.

## Notas adicionais

* Os comentários dos pedidos seguem o formato `BotName;SymbolId;YYYYMMDD`, o que facilita a distinção dos pedidos pendentes criados em dias diferentes.
* A estratégia assina os dados do Nível 1 para avaliar o spread atual do filtro e rastrear stops com os últimos valores de compra/venda.
* Os trailing stops são reaplicados em cada atualização de vela e imediatamente após os preenchimentos para corresponder ao comportamento original EA.
