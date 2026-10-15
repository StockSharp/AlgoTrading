# Sistema Expert Advisor da Camada Polonesa Eficiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta direta do consultor especialista MQL4 "Polish Layer Expert Advisor System Efficient". Ele foi projetado para gráficos intradiários (o autor original recomendou velas de 5 ou 15 minutos) e restringe a negociação a uma única posição por vez. A direção da tendência é definida pelo alinhamento entre uma média móvel de preço rápida e lenta junto com dois filtros RSI suavizados. As entradas reais requerem uma confirmação tripla dos indicadores Stochastic Oscillator, DeMarker e Williams %R para capturar reversões de condições extremas que ocorrem dentro da tendência predominante.

## Lógica de negociação
1. **Filtro de tendência.** Uma média móvel simples de 9 períodos (SMA) de preços de fechamento deve estar acima da média móvel ponderada linear (LWMA) de 45 períodos para permitir posições compradas e abaixo dela para permitir posições vendidas. Ao mesmo tempo, o período de 9 SMA de RSI deve estar acima (para posições compradas) ou abaixo (para posições curtas) do período de 45 SMA de RSI. Qualquer divergência entre o preço e os filtros RSI bloqueia novos pedidos.
2. **Stochastic gatilho.** Quando o filtro de tendência é de alta, a estratégia espera que a linha Stochastic %K cruze para cima acima do limite de sobrevenda (padrão 19) e simultaneamente cruze acima de %D. Para configurações de baixa, %K deve cruzar abaixo do limite de sobrecompra (padrão 81) e cair abaixo de %D. O fator de lentidão é preservado do script MQL4.
3. **Confirmações de impulso.** Um sinal longo requer adicionalmente que DeMarker cruze para cima até 0,35 e Williams %R cruze para cima até -81 na vela concluída atual. Sinais curtos exigem cruzamentos descendentes em 0,63 e -19, respectivamente. Todos os cruzamentos são avaliados entre a vela finalizada anterior e a atual.
4. **Gerenciamento de posição.** Somente ordens de mercado são emitidas e a estratégia permanece estável até que um stop ou alvo de proteção feche a negociação. Os níveis de proteção são recalculados a partir de parâmetros baseados em pip usando a etapa de preço do instrumento. Se a etapa de preço não estiver disponível, a proteção será desativada.

## Gestão de risco
* **Stop-loss / take-profit.** As distâncias são configuradas em pips. Quando positivos, os valores são convertidos em compensações de preço reais usando `Security.PriceStep` (1 pip = 1 etapa de preço) e aplicados imediatamente após a entrada. Definir um parâmetro como `0` desativa o nível de proteção correspondente.
* **Posição única.** O EA original nunca foi piramidado, portanto a porta se recusa a entrar se já existir uma posição.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Volume` | `0.1` | Volume do pedido em lotes. Ajuste de acordo com o tamanho do contrato da corretora. |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Tipo de vela usado para cálculos de indicadores. Defina um período de 5 ou 15 minutos para espelhar o EA original. |
| `RsiPeriod` | `14` | Comprimento de lookback para o cálculo base RSI. |
| `ShortPricePeriod` | `9` | Período do preço rápido SMA usado no filtro de tendência. |
| `LongPricePeriod` | `45` | Período do LWMA de preço lento usado no filtro de tendência. |
| `ShortRsiPeriod` | `9` | Comprimento do SMA rápido aplicado a valores RSI. |
| `LongRsiPeriod` | `45` | Comprimento do SMA lento aplicado a valores RSI. |
| `StochasticKPeriod` | `5` | Período base %K para o oscilador Stochastic. |
| `StochasticDPeriod` | `3` | Período de suavização para a linha %D. |
| `StochasticSlowing` | `3` | Fator de suavização adicional aplicado a %K. |
| `DemarkerPeriod` | `14` | Período médio para o indicador DeMarker. |
| `WilliamsPeriod` | `14` | Período de lookback para Williams %R. |
| `StochasticOversoldLevel` | `19` | Limite de sobrevenda que %K deve ultrapassar para cima para permitir entradas longas. |
| `StochasticOverboughtLevel` | `81` | Limite de sobrecompra que %K deve ultrapassar para baixo para permitir entradas curtas. |
| `DemarkerBuyLevel` | `0.35` | Valor mínimo de DeMarker necessário para entradas longas (cruzamento por baixo). |
| `DemarkerSellLevel` | `0.63` | Valor máximo de DeMarker permitido para entradas curtas (cruzamento de cima). |
| `WilliamsBuyLevel` | `-81` | Williams %R cruzando o nível confirmando entradas longas. |
| `WilliamsSellLevel` | `-19` | Williams %R cruzando o nível confirmando entradas curtas. |
| `StopLossPips` | `7777` | Distância de stop-loss em pips. O padrão muito grande desativa efetivamente a parada, a menos que seja configurado. |
| `TakeProfitPips` | `17` | Distância de lucro em pips. Defina como `0` para desativar o alvo fixo. |

## Notas
* Certifique-se de que `Security.PriceStep`, `Security.MinVolume` e `Security.VolumeStep` estejam configurados corretamente; a estratégia assume que um pip é igual a um passo de preço ao converter os parâmetros de risco.
* Os filtros de entrada dependem de cruzamentos de indicadores entre velas consecutivas concluídas. Ao importar dados históricos, mantenha o alinhamento da barra idêntico ao período original para reproduzir os resultados.
