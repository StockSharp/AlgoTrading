# Estratégia média corrigida do canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de canal média corrigida** é uma versão C# do MetaTrader consultor especialista `e-CA-5`. O sistema reconstrói o indicador "Média Corrigida" (CA) toda vez que uma vela fecha e abre uma posição quando o preço cruza a média móvel corrigida por um deslocamento sigma configurável. A implementação convertida depende da vela de alto nível API de API, usa ordens de mercado e gerencia saídas de proteção (stop-loss, take-profit, trailing stop) internamente para espelhar o comportamento do Expert Advisor original.

## Corrected Average indicator
O filtro CA combina uma média móvel com feedback de volatilidade. A versão MQL expõe três entradas: comprimento da média móvel, método de cálculo da média e preço aplicado. Na porta StockSharp:

1. O tipo de média móvel é selecionado através do parâmetro `MaTypeOption` (SMA, EMA, SMMA, LWMA) e do comprimento `MaPeriod`.
2. Um indicador `StandardDeviation` com o mesmo período mede a volatilidade atual.
3. Para cada vela finalizada, o valor corrigido é calculado iterativamente:
   - Seja `M_t` o valor MA na barra mais recente e `CA_{t-1}` o valor corrigido da barra anterior.
   - Calcule `v1 = StdDev_t^2` e `v2 = (CA_{t-1} - M_t)^2`.
   - Se `v2 <= 0` ou `v2 < v1`, mantenha o fator de correção `k = 0`. Caso contrário, defina `k = 1 - v1 / v2`.
   - Atualize `CA_t = CA_{t-1} + k * (M_t - CA_{t-1})`.
   - O primeiro valor corrigido é padronizado para a própria média móvel.

Este ciclo de feedback amortece o MA durante períodos de silêncio e permite ajustes rápidos quando o preço diverge além da estimativa de volatilidade atual.

## Lógica de negociação
1. A estratégia assina o tipo de vela configurado (`CandleType`) e espera até que a média móvel e o desvio padrão estejam totalmente formados.
2. Assim que uma vela termina, o algoritmo calcula o novo valor corrigido e compara o fechamento da vela anterior com o nível corrigido anterior.
3. Dois deslocamentos sigma, `SigmaBuyPoints` e `SigmaSellPoints`, são convertidos em distâncias de preço usando o `PriceStep` do instrumento.
4. As regras de entrada usam o fechamento da vela anterior e o nível corrigido recentemente calculado:
   - **Compre** se o fechamento anterior estiver abaixo da média corrigida mais o sigma de compra, e o fechamento atual terminar acima desse limite superior.
   - **Venda** se o fechamento anterior estiver acima da média corrigida menos o sigma de venda e o fechamento atual terminar abaixo desse limite inferior.
5. Apenas uma posição líquida é permitida. Uma nova negociação é submetida apenas quando não há exposição.

Como a versão StockSharp opera em velas finalizadas, a confirmação do rompimento ocorre uma vez por barra, em vez de a cada tick, fornecendo comportamento determinístico adequado para backtesting e automação ao vivo com dados de velas.

## Gestão de risco
A porta reproduz todas as três mecânicas de proteção do Expert Advisor original:

- **Stop-loss fixo**: `StopLossPoints` multiplicado pela etapa de preço define a distância entre o preço de entrada e o stop de proteção. Um stop acionado fecha toda a posição com uma ordem de mercado.
- **Take-profit fixo**: `TakeProfitPoints` converte em uma distância alvo de lucro. Quando o preço atinge o nível durante uma vela, a posição é fechada com uma ordem de mercado.
- **Trailing stop**: Quando `TrailingPoints` é maior que zero, a estratégia rastreia o lucro não realizado e, uma vez que o preço tenha avançado pelo menos essa distância, armazena um nível móvel atrás do último fechamento. O trailing stop apenas avança e honra `TrailingStepPoints`, o que representa a melhoria mínima antes que um novo nível móvel seja aceito. Os níveis finais são arredondados com `Security.ShrinkPrice` para que se alinhem com o tamanho do tick do instrumento.

Todas as saídas redefinem o estado de risco interno. Quando o próximo sinal aparece, os níveis stop, alvo e trailing são recalculados a partir do novo preço de preenchimento, garantindo um comportamento próximo à versão MQL que modifica as proteções da ordem original.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `OrderVolume` | Quantidade utilizada para entradas no mercado. Deve ser positivo. |
| `TakeProfitPoints` | Meta de lucro em etapas de preço (0 desativa o take-profit). |
| `StopLossPoints` | Distância de stop-loss em etapas de preço (0 desativa o stop-loss). |
| `TrailingPoints` | Distância de lucro (em etapas de preço) necessária antes da ativação do trailing stop. |
| `TrailingStepPoints` | Distância extra mínima que deve ser capturada antes de mover o stop móvel novamente. |
| `MaPeriod` | Período da média móvel e do desvio padrão. |
| `MaTypeOption` | Tipo de média móvel: SMA, EMA, SMMA ou LWMA. |
| `SigmaBuyPoints` | O deslocamento Sigma foi adicionado acima da média corrigida antes de abrir uma posição longa. |
| `SigmaSellPoints` | Compensação Sigma subtraída abaixo da média corrigida antes de abrir uma posição curta. |
| `CandleType` | Série de velas usada para cálculos de indicadores e avaliação de sinal. |

Todos os parâmetros numéricos suportam otimização por meio de `SetCanOptimize(true)` para que a estratégia possa ser calibrada diretamente dentro do ambiente StockSharp.

## Notas de uso
- O tipo de vela padrão é uma hora. Ajuste-o para corresponder ao período usado ao otimizar a estratégia MetaTrader original.
- `Security.PriceStep` é usado para traduzir todas as entradas de "pontos" em distâncias de preços reais. Instrumentos sem uma etapa configurada voltam para `1`, preservando o comportamento sensato para índices ou criptomoedas.
- The strategy executes only on finished candles. Se for necessária precisão intrabarra, reduza o período para a granularidade desejada.
- Os trailing stops são implementados com ordens de mercado quando violados, imitando o EA original que modificou os preços do stop-loss. Esta abordagem evita a colocação de ordens stop adicionais e mantém a gestão de risco contida na própria estratégia.
- Nenhuma versão do Python é fornecida para esta conversão, de acordo com os requisitos da tarefa.

## Diferenças do original EA
- O API baseado em vela de StockSharp substitui o processamento em nível de tick; todas as decisões são tomadas quando uma vela se fecha.
- O gerenciamento de pedidos é compensado: posições opostas não são mantidas simultaneamente, correspondendo à lógica de pedido único da versão MetaTrader.
- As paradas protetoras e as saídas finais são executadas por meio de ordens de mercado, em vez de modificar os tickets de ordem existentes. Este comportamento é equivalente em contas de compensação, mantendo a implementação consistente com outras estratégias StockSharp.

Essas adaptações preservam a ideia comercial de `e-CA-5` enquanto alinham a lógica com as práticas recomendadas de StockSharp e as convenções de alto nível API descritas nas diretrizes do repositório.
