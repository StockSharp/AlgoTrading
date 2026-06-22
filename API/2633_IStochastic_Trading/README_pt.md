# Estratégia de Trading IStochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Trading IStochastic é um port direto do StockSharp do assessor especializado "IStochastic_Trading" do MetaTrader 5. O bot usa o Oscilador Estocástico para detectar condições de sobrecompra e sobrevenda e depois constrói uma escada de posições no estilo martingale enquanto gerencia cada entrada com stop-loss, take-profit e um trailing stop. A implementação opera em velas concluídas obtidas através da API de alto nível do StockSharp e depende apenas de ordens de mercado.

## Lógica de trading
1. Calcular um Oscilador Estocástico com comprimento %K, suavização %D e um fator de retardamento adicional configuráveis.
2. Quando não há posições ativas, avaliar a vela concluída mais recente:
   - Abrir uma posição comprada se %K estiver acima de %D e %D estiver abaixo da zona de compra configurada.
   - Abrir uma posição vendida se %K estiver abaixo de %D e %D estiver acima da zona de venda configurada.
3. Quando uma posição existe, monitorar o último preenchimento na escada:
   - Se o mercado se mover contra a operação pelo menos o gap configurado (em pips), abrir uma nova posição na mesma direção com o dobro do volume anterior, desde que o número máximo de posições não seja excedido.
4. Para cada entrada manter níveis de stop-loss e take-profit por operação derivados de distâncias em pips convertidas para pontos de preço usando o `PriceStep` do instrumento e o número de decimais. Se o preço de fechamento atingir o stop ou o alvo, a estratégia sai da posição específica com uma ordem de mercado.
5. Aplicar um trailing stop após cada fechamento de vela. Quando a operação se move suficientemente na direção favorável, o preço do stop é ajustado pelo passo de trailing especificado, aproximando o comportamento de trailing por posição do terminal.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Tamanho inicial da posição em lotes. Entradas adicionais dobram o volume anterior. |
| `TakeProfitPips` | `50` | Distância do take-profit medida em pips. O valor é convertido para pontos de preço internamente. |
| `StopLossPips` | `50` | Distância do stop-loss em pips para cada posição. |
| `TrailingStopPips` | `10` | Distância do trailing stop em pips. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | `5` | Movimento favorável mínimo (em pips) antes de o trailing stop ser ajustado. |
| `MaxPositions` | `3` | Número máximo de passos martingale simultaneamente abertos. Um valor de `0` remove o limite. |
| `GapPips` | `7` | Gap de preço, em pips, necessário antes de dobrar na direção atual. |
| `KPeriod` | `5` | Número de velas usadas para construir a linha %K. |
| `DPeriod` | `3` | Período da média de suavização %D. |
| `Slowing` | `3` | Suavização adicional aplicada a %K. |
| `ZoneBuy` | `30` | Limiar de %D usado para validar entradas compradas (zona de sobrevenda). |
| `ZoneSell` | `70` | Limiar de %D usado para validar entradas vendidas (zona de sobrecompra). |
| `CandleType` | `Período de 15 minutos` | Série de velas empregada para os cálculos. |

## Notas de implementação
- As distâncias em pips são convertidas para preços com `PriceStep`. Para cotações de 3 e 5 dígitos um fator adicional de 10 é usado para imitar a lógica de ponto ajustado do MetaTrader.
- As verificações de stop-loss, take-profit e trailing stop dependem dos preços de fechamento das velas para manter a lógica determinística dentro do backtester. A execução em tempo real pode ser personalizada se o gerenciamento intrabar for necessário.
- A estratégia abre apenas uma escada direcional por vez; todas as posições devem ser fechadas antes de mudar de direção.
- A implementação em Python é intencionalmente omitida conforme solicitado.
