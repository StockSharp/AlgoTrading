# Estratégia de Estabilização de Pedidos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Estabilização de Pedidos** é uma conversão do MetaTrader consultor especialista `hjueiisyx8lp2o379e_www_forex-instruments_info.mq4`. O robô original coloca um par de ordens stop em torno do preço atual e aguarda um rompimento. Depois que uma posição é aberta, o sistema monitora os corpos recentes das velas para determinar se a ação do preço estagnou ("estabilizada") e sai da negociação quando o mercado perde impulso ou quando um limite de lucro predefinido é atingido.

Esta porta C# mantém a mesma lógica usando o StockSharp API de alto nível. Ele se baseia em velas concluídas em vez de ticks brutos, tornando o comportamento determinístico durante backtesting e negociações ao vivo.

## Regras de negociação
1. Quando não há posições abertas ou ordens ativas, a estratégia apresenta um **stop de compra** acima do mercado e um **stop de venda** abaixo do mercado. A distância é medida em MetaTrader pontos (geralmente igual a um pip).
2. Se uma ordem de parada for executada:
   - A ordem preenchida abre uma posição de `OrderVolume` lotes.
   - A ordem de stop oposta permanece pendente para detectar um rompimento na outra direção.
3. Enquanto uma posição está aberta, a estratégia verifica o tamanho do corpo das duas velas finalizadas mais recentes:
   - Se o último corpo da vela for menor que `StabilizationPoints` e o lucro flutuante for maior que `ProfitThreshold`, a posição será fechada e a ordem pendente oposta será cancelada.
   - Se duas velas consecutivas forem menores que `StabilizationPoints`, a negociação será fechada independentemente do lucro atual.
   - Se o lucro atingir `AbsoluteFixation`, a negociação será encerrada imediatamente.
4. Os pedidos pendentes são cancelados e recriados após `ExpirationMinutes`, a menos que o valor seja definido como zero (vida útil infinita).

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume de negociação em lotes utilizados para ambas as entradas de stop. | `0.1` |
| `OrderDistancePoints` | Distância entre o preço de fechamento atual e cada ordem de stop, expressa em MetaTrader pontos. | `20` |
| `ProfitThreshold` | Lucro flutuante mínimo (moeda da conta) exigido antes que uma saída desencadeada pela estabilização seja permitida. | `-2` |
| `AbsoluteFixation` | Nível de lucro (moeda da conta) que força uma saída imediata. | `30` |
| `StabilizationPoints` | Tamanho máximo do corpo da vela (pontos) que sinaliza um mercado estável. | `25` |
| `ExpirationMinutes` | Vida útil das ordens stop pendentes em minutos. `0` desativa a expiração. | `20` |
| `CandleType` | Tipo de vela usado para avaliar a estabilização (o padrão é um período de 5 minutos). | `TimeFrame(5m)` |

## Notas de conversão
- O consultor especialista original operava com base nos ticks do gráfico. Esta porta avalia apenas velas finalizadas, preservando a lógica e garantindo backtests reproduzíveis.
- MetaTrader "pontos" são mapeados para StockSharp `PriceStep`. Se o instrumento não tiver um passo de preço, um passo de `1` será assumido.
- O lucro é aproximado usando `PriceStep` e `StepPrice` para traduzir o movimento do preço na moeda da conta.
- Todos os comentários do código foram reescritos em inglês e os metadados dos parâmetros incluem descrições fáceis de usar com agrupamento.

## Uso
1. Adicione a estratégia à sua solução StockSharp e atribua a segurança e o portfólio desejados.
2. Configure os parâmetros, especialmente o intervalo de tempo da vela e a distância em pontos, para corresponder às características do instrumento.
3. Comece a estratégia. Ele enviará ordens de stop emparelhadas e gerenciará posições de acordo com a lógica de estabilização descrita acima.

## Mais ideias
- Experimente diferentes intervalos de velas para equilibrar a capacidade de resposta e a filtragem de ruído.
- Combine a estratégia com filtros de volatilidade (ATR, Bollinger Bandas) para evitar negociações durante sessões extremamente silenciosas.
- Estenda a lógica com trailing stops ou saídas parciais de posição quando a meta de lucro absoluto for atingida.
