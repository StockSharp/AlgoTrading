# Estratégia do Caranguejo Farhad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Farhad Crab Strategy** é uma versão StockSharp de alto nível do MetaTrader consultor especialista `FarhadCrab1.mq4`. O EA original é um sistema de scalping rápido projetado para o período M1 em GBP/JPY, GBP/USD e EUR/USD. Essa conversão recria a lógica de negociação em C#, combinando filtros de média móvel intradiários com uma rede de segurança de tendência diária e gerenciamento de saída automatizado.

A estratégia analisa o período atual através de um EMA de 9 períodos calculado sobre o preço típico e um SMA de 9 períodos calculado sobre a abertura da vela. Ao mesmo tempo, ele acompanha uma média móvel suavizada de 55 períodos (SMMA) construída a partir de velas diárias. Sempre que os filtros de curto prazo mostram impulso suficiente para cima enquanto nenhuma posição está aberta, uma negociação longa é acionada. Por outro lado, quando a máxima intradiária permanece abaixo de SMA de aberturas, uma negociação curta é aberta. O SMMA diário atua como uma sobreposição protetora: cruzar o preço por baixo força a saída de todas as negociações longas e cruzar por cima fecha as posições curtas.

O gerenciamento de saída reproduz o comportamento original do EA com níveis configuráveis de take-profit em pips e trailing stops independentes para posições longas e curtas. A lógica de trailing segue a implementação MetaTrader movendo o stop somente após o mercado avançar pela distância configurada. A estratégia fecha posições por meio de ordens de mercado em vez de ordens de stop pendentes, tornando-a compatível com o fluxo de eventos de alto nível API.

## Principais recursos

- **Conjunto de indicadores idêntico ao EA** – EMA de 9 períodos no preço típico, SMA de 9 períodos em aberturas e um SMMA diário de 55 períodos para direção da tendência.
- **Tratamento de dados de vários períodos de tempo** – assina o período de negociação e as velas diárias simultaneamente, permitindo que StockSharp calcule os indicadores necessários sem buffer manual.
- **Saídas configuráveis** – distâncias de take-profit simétricas (longas/curtas) e trailing stops expressos em pips, assim como as entradas externas originais.
- **Chave de segurança diária** – replica a regra do EA que fecha posições compradas quando o SMMA diário se move acima do fechamento diário e vendas quando se move abaixo.
- **Proteção integrada** – chama `StartProtection()` uma vez na inicialização para proteger posições de acordo com as práticas recomendadas da estrutura.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume de negociação aplicado a novas ordens de mercado. | `0.1` |
| `LongTakeProfitPips` | Distância de take-profit para posições longas, medida em pips. | `10` |
| `ShortTakeProfitPips` | Distância de take-profit para posições curtas, medida em pips. | `10` |
| `LongTrailingStopPips` | Distância de parada final para negociações longas. O rastreamento é desativado quando definido como zero. | `8` |
| `ShortTrailingStopPips` | Distância de parada móvel para negociações curtas. O rastreamento é desativado quando definido como zero. | `8` |
| `DailyMaPeriod` | Comprimento da média móvel suavizada diária usada para saídas de proteção. | `55` |
| `CandleType` | Prazo principal que orienta os cálculos da estratégia. O padrão é velas de 1 minuto. | `1m` |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` e marcados como otimizáveis onde faz sentido, para que possam ser ajustados por meio do otimizador StockSharp.

## Regras de negociação

1. **Entradas longas**: Quando o mínimo da vela atual permanecer acima do período de 9 EMA do preço típico e nenhuma posição estiver ativa, abra uma negociação longa.
2. **Entradas curtas**: Quando a máxima atual da vela permanecer abaixo dos 9 períodos SMA do preço de abertura e nenhuma posição estiver ativa, abra uma negociação curta.
3. **Saída de proteção diária (longa)**: feche qualquer posição longa se o SMMA diário se mover acima do fechamento diário enquanto anteriormente estava abaixo do fechamento anterior.
4. **Saída de proteção diária (curta)**: Feche qualquer posição curta se o SMMA diário se mover abaixo do fechamento diário enquanto anteriormente estava acima do fechamento anterior.
5. **Take-profit**: feche a posição assim que a meta de pip configurada for atingida.
6. **Trailing stop**: Depois que uma posição ganha a distância final, garanta os lucros monitorando a distância de retração e saia quando o preço recuar nesse valor.

## Notas de implementação

- O código depende exclusivamente de chamadas `SubscribeCandles().Bind(...)` de alto nível, eliminando quaisquer buffers de indicadores manuais e permanecendo dentro das diretrizes do projeto.
- Os pips são calculados a partir do `PriceStep` do instrumento com o ajuste usual no estilo MetaTrader para cotações de 3 e 5 dígitos. Isso mantém o comportamento consistente com os parâmetros baseados em pontos do EA.
- A gestão de stop-loss e take-profit é realizada internamente, fechando posições quando as condições são atendidas, em vez de registrar ordens de limite/stop. Esta abordagem corresponde às saídas instantâneas encontradas no script original, permanecendo compatível com a execução assíncrona de pedidos em StockSharp.
- A estratégia redefine seu estado dentro de `OnReseted`, garantindo que as execuções de otimização e lançamentos repetidos comecem do zero.

## Dicas de uso

- O EA original foi adaptado para pares GBP e EUR altamente voláteis no período M1. Resultados semelhantes podem ser esperados quando se aplicam os mesmos prazos e instrumentos, mas os parâmetros estão expostos para acomodar diferentes perfis de volatilidade.
- Como o sistema mantém apenas uma posição por vez, ele é adequado para backtesting direto e execução ao vivo, sem pirâmides complexas de posições.
- Os trailing stops tornam-se mais eficazes em instrumentos com tendências suaves. Em mercados variados, considere reduzir a distância de fuga ou confiar apenas em saídas com fins lucrativos.
- A saída diária do SMMA serve como controle primário de risco. Para configurações orientadas para swing, você pode aumentar `DailyMaPeriod` para tornar o filtro de longo prazo menos reativo.
