# Média por estratégia de sinal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de média por sinal** transporta o MetaTrader especialista `AveragingBySignal.mq4` para o StockSharp API de alto nível. O consultor original combinou um filtro de entrada cruzada de média móvel com média no estilo Martingale, uma cesta compartilhada de lucro e um trailing stop opcional que é ativado apenas para o primeiro pedido. Esta versão C# recria os mesmos blocos de construção enquanto os adapta ao modelo de execução de rede e à estrutura de indicadores de StockSharp.

## Lógica de negociação
1. Assine o período configurado (`CandleType`) e alimente duas médias móveis construídas com os períodos e métodos solicitados (`FastPeriod`/`FastMethod`, `SlowPeriod`/`SlowMethod`).
2. Aguarde as velas totalmente fechadas. Quando uma barra for concluída, compare os valores anteriores e atuais de ambas as médias para detectar um cruzamento rápido/lento.
3. Gerar sinais:
   - um cruzamento de alta (aumento rápido acima do lento) produz um sinal longo;
   - um cruzamento de baixa (queda rápida abaixo da lenta) produz um sinal curto;
   - caso contrário, a estratégia permanecerá ociosa.
4. Em um novo sinal longo e enquanto nenhuma cesta longa estiver ativa, envie uma ordem de compra de mercado usando o volume base retornado pelo bloco de dimensionamento de posição.
5. Com um novo sinal de venda a descoberto e enquanto nenhuma cesta de venda a descoberto estiver ativa, envie uma ordem de venda a mercado.
6. Regras de média:
   - a distância até a próxima camada é controlada por `LayerDistancePips` convertido em pips no estilo MetaTrader;
   - camadas longas adicionais requerem um sinal de alta (quando `AveragingBySignal` for verdadeiro) ou apenas a condição de preço (quando falso);
   - camadas curtas adicionais seguem a lógica simétrica;
   - o tamanho do lote de cada nova camada é calculado com o modo `LotSizing` e limitado a `MaxLayers` entradas por direção.
7. Gerenciamento de cesta:
   - cada negociação preenchida é rastreada na ordem FIFO para reconstruir o preço médio de entrada das cestas longas e curtas;
   - o preço médio ponderado mais/menos `TakeProfitPips` forma o lucro compartilhado. Quando o preço de fechamento atinge esse nível, toda a cesta é fechada;
   - se `EnableTrailing` estiver ativado e existir exatamente uma ordem em uma cesta, um trailing stop é armado após `TrailingStartPips` de lucro flutuante. O stop é avançado sempre que o preço melhora em pelo menos `TrailingStepPips`.
8. A estratégia funciona num ambiente de compensação: sinais opostos compensam automaticamente a exposição existente antes de abrir o próximo cesto.

## Dimensionamento de posição e cálculo de pip
- `InitialVolume` define o lote base. Quando `LotSizing` é definido como `Multiplier`, cada camada adicional multiplica o lote base por `Multiplier^layerIndex`, reproduzindo a lógica MQL `LotType`.
- O auxiliar ajusta o volume solicitado para `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para que cada pedido seja compatível com a troca.
- Os valores de pip são derivados de `Security.PriceStep` e imitam o ajuste original de "dois dígitos": os símbolos FX de cinco dígitos usam 0,0001, enquanto os símbolos de quatro dígitos usam 0,0001 como estão.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 hora | Prazo principal para cálculos de indicadores. |
| `InitialVolume` | `decimal` | `0.1` | Tamanho base do lote para o primeiro pedido em uma cesta. |
| `LotSizing` | `LotSizingMode` | `Multiplier` | Escolha entre lotes fixos ou escala geométrica. |
| `Multiplier` | `decimal` | `2` | Multiplicador de lote aplicado a cada camada extra quando `LotSizing` = `Multiplier`. |
| `FastPeriod` | `int` | `28` | Lookback da média móvel rápida. |
| `FastMethod` | `MovingAverageMethod` | `LinearWeighted` | Método de média móvel para a linha rápida. |
| `SlowPeriod` | `int` | `50` | Retrospectiva da média móvel lenta. |
| `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Método de média móvel para a linha lenta. |
| `TakeProfitPips` | `int` | `15` | Distância de take-profit compartilhada para toda a cesta (0 desabilita). |
| `AveragingBySignal` | `bool` | `true` | Exija um novo sinal antes de adicionar novas camadas. |
| `LayerDistancePips` | `decimal` | `10` | Movimento adverso mínimo (em pips) antes da média. |
| `MaxLayers` | `int` | `10` | Máximo de pedidos simultâneos por sentido, incluindo o inicial. |
| `EnableTrailing` | `bool` | `false` | Habilite o trailing stop para cestas de pedido único. |
| `TrailingStartPips` | `decimal` | `10` | Lucro flutuante necessário antes do início do trailing. |
| `TrailingStepPips` | `decimal` | `1` | Progresso adicional necessário para mover o stop móvel. |

## Diferenças do consultor especialista original
- StockSharp opera em modo de compensação, enquanto MetaTrader 4 permitiu posições de hedge independentes. Quando um sinal muda de direção, a nova ordem de mercado compensa a exposição existente antes de criar uma nova cesta.
- O take-profit compartilhado é implementado como um comando de saída explícito em vez de modificar cada ticket com `OrderModify`.
- O trailing stop é modelado com saídas de mercado acionadas pelos preços de fechamento das velas. O especialista original baseava-se em atualizações de stop em nível de tick; portanto, a versão C# pode ficar um pouco mais tarde, mas segue os mesmos limites.
- Verificações de risco como `AccountFreeMarginCheck` e tratamento de derrapagem são omitidas porque os corretores StockSharp aplicam regras de margem/preço diretamente.

## Dicas de uso
- Forneça metadados precisos do instrumento (`PriceStep`, `VolumeStep`, volume mínimo e máximo) para conversões corretas de pip e volume.
- Mantenha `FastPeriod` estritamente inferior a `SlowPeriod`; a estratégia para automaticamente se a configuração impedir cruzamentos válidos.
- Desative `AveragingBySignal` quando desejar uma grade pura que reaja apenas aos níveis de preços, independentemente do cruzamento mais recente.
- Como a lógica de saída opera em velas fechadas, prazos mais baixos produzem reações mais rápidas, mas também podem aumentar o ruído e o número de camadas médias.
