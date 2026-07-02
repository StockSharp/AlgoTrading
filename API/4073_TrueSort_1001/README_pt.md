# Estratégia TrueSort 1001
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

TrueSort 1001 é um sistema estrito de acompanhamento de tendências que reflete o consultor especialista MQL original. A estratégia observa cinco médias móveis simples e só atua quando elas permanecem perfeitamente ordenadas por três velas consecutivas concluídas. Um Índice Direcional Médio crescente (ADX) confirma o impulso antes de qualquer negociação ser aberta. Uma vez no mercado, a posição é protegida por um trailing stop adaptativo medido em etapas de preço e a negociação é fechada assim que as médias móveis perdem o alinhamento.

## Lógica

### Filtro de tendência e impulso
- Cinco SMAs (10, 20, 50, 100 e 200 períodos por padrão) são calculados no período selecionado.
- Para configurações longas, os SMAs rápidos devem estar estritamente acima dos mais lentos em cada uma das últimas três velas finalizadas: `SMA10 > SMA20 > SMA50 > SMA100 > SMA200`.
- Para configurações curtas, a ordem oposta é necessária nas mesmas três velas.
- ADX com período `AdxPeriod` deve ficar acima de `AdxThreshold` e o valor atual deve ser maior que a vela anterior, garantindo que a força da tendência esteja aumentando.

### Condições de entrada
1. Nenhuma posição está aberta.
2. Três velas históricas satisfazem a regra de ordenação descrita acima.
3. O filtro ADX é aprovado.
4. Uma ordem de mercado de `Volume` lotes é enviada imediatamente no fechamento da vela atual.

### Condições de saída
- **Dessincronização da média móvel:** quando a vela atual fecha e a pilha MA não está mais estritamente ordenada na direção da negociação, a posição é liquidada.
- **Proteção de rastreamento:** `StopLossPoints` são convertidos em distância de preço absoluto multiplicando pelo instrumento `PriceStep`. Para negociações longas, o stop é inicializado no máximo entre `SMA100` e `Close - distance`. Para shorts é o mínimo entre `SMA100` e `Close + distance`. Após cada vela, o stop é reduzido em direção ao preço, mas nunca afrouxado. Se o preço ultrapassar o stop, a posição será fechada no mercado.

### Notas adicionais
- Todas as decisões são tomadas apenas em velas acabadas; velas inacabadas são ignoradas.
- O algoritmo armazena os últimos três valores SMA internamente para replicar a lógica `shift` do script MQL original sem solicitar o histórico do indicador.
- Os valores de ADX são processados via `BindEx` e a negociação é tentada apenas quando a estratégia está online e os dados estão totalmente formados.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `0.1` | Tamanho do pedido em lotes para cada entrada no mercado. |
| `StopLossPoints` | `100` | Distância do trailing-stop expressa em etapas de preço do instrumento. `0` desativa o rastreamento. |
| `Sma10Length` | `10` | Período do mais rápido SMA. |
| `Sma20Length` | `20` | Período do segundo SMA. |
| `Sma50Length` | `50` | Período do meio SMA. |
| `Sma100Length` | `100` | Período utilizado tanto para alinhamento quanto para referência de parada inicial. |
| `Sma200Length` | `200` | Mais lento SMA confirmando a tendência de longo prazo. |
| `AdxPeriod` | `14` | Período do indicador ADX. |
| `AdxThreshold` | `25` | Nível mínimo ADX e condição ascendente necessária antes das entradas. |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Série de velas usada para todos os cálculos de indicadores. |

## Detalhes de implementação
- O código depende da assinatura de vela de alto nível StockSharp e vincula seis indicadores (cinco SMAs e ADX) em um único pipeline.
- Os buffers de histórico com comprimento três armazenam os valores SMA mais recentes, evitando chamadas para `GetValue()` enquanto mantêm a paridade exata com as mudanças MQL.
- Os trailing stops são gerenciados manualmente; `StartProtection()` ainda está ativado, portanto a infraestrutura padrão estará pronta caso sejam necessárias proteções adicionais.
- Os comentários dentro do código explicam cada etapa em inglês para facilitar a manutenção.
