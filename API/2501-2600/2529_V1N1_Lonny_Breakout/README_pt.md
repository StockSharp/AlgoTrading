# Estratégia V1N1 Lonny Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia V1N1 Lonny Breakout replica o consultor especialista do MetaTrader "V1N1 LONNY". Tem como alvo rompimentos que emergem em torno das sessões de Londres e Nova York, construindo um intervalo de abertura e esperando por um fechamento decisivo fora desse intervalo. A estratégia se baseia em uma média móvel exponencial para capturar a tendência predominante e em um oscilador estocástico para filtrar condições de sobrecompra ou sobrevenda antes de entrar no mercado.

Um modelo de risco configurável permite dimensionar posições por volume fixo ou como porcentagem do capital da conta. A implementação também inclui filtragem de spread opcional, trailing stops e um timeout baseado em barras que fecha a operação se o momentum desaparecer após um número predefinido de velas.

## Lógica de trading
1. **Alinhamento de sessão** – O trading só é permitido entre os horários de início e fim configurados. O cronograma pode ser ajustado de acordo com os horários de verão para Londres ou Nova York.
2. **Intervalo de abertura** – Imediatamente antes do início da sessão, a estratégia registra as máximas e mínimas de um número fixo de velas. Este intervalo fornece os níveis de rompimento usados durante a janela de trading.
3. **Confirmação de tendência** – A inclinação da média móvel exponencial (EMA) deve concordar com a direção da operação. Um rompimento de alta requer que a EMA suba, enquanto um rompimento de baixa requer que caia.
4. **Filtro de momentum** – O oscilador estocástico deve permanecer dentro de uma zona configurável em torno do ponto médio para evitar entrar quando o mercado já está sobrecomprado ou sobrevendido.
5. **Validação do rompimento** – A vela anterior deve fechar além da máxima ou mínima do intervalo pelo menos a distância mínima de rompimento, mas não mais longe que a distância máxima.
6. **Controles de risco** – Cada posição define um stop loss a partir do limite do intervalo e um alvo de take-profit baseado em um fator dessa distância de stop. Um trailing stop pode apertar a saída conforme a operação avança, e as posições podem ser fechadas forçosamente após um certo número de velas.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `StartTrade` | Hora de início da sessão. |
| `EndTrade` | Hora de fim da sessão. |
| `SwitchDst` | Tratamento de horário de verão: Europa (sem mudança), EUA (mudança relativa entre Londres e Nova York), ou desativado. |
| `RiskModes` | Modo de dimensionamento de posição (porcentagem do capital ou volume fixo). |
| `PositionRisk` | Porcentagem de risco ou volume fixo, dependendo do modo. |
| `TradeRange` | Número de velas usadas para construir o intervalo de abertura. |
| `MinRangePoints` / `MaxRangePoints` | Tamanho mínimo e máximo do intervalo de abertura, em pontos de preço. |
| `MinBreakRange` / `MaxBreakRange` | Distância de rompimento mínima e máxima aceitável acima ou abaixo do intervalo, em pontos de preço. |
| `StopLossPoints` | Distância do stop-loss medida a partir do lado oposto do intervalo, em pontos de preço. |
| `TpFactor` | Multiplicador de take-profit aplicado à distância do stop-loss. |
| `TrailStopPoints` | Distância opcional do trailing stop, em pontos de preço. Definir como zero para desabilitar o trailing. |
| `TrendPeriod` | Período para o filtro de inclinação EMA. |
| `OverPeriod` | Período para o oscilador estocástico. |
| `OverLevels` | Distância a partir de 50 usada para definir o intervalo aceitável do estocástico. |
| `BarsToClose` | Número máximo de velas para manter a posição aberta. Zero desabilita o timeout. |
| `MaxSpreadPoints` | Spread máximo permitido em pontos de preço. |
| `SlippagePoints` | Slippage de referência em pontos de preço (mantido por compatibilidade com o consultor especialista original). |
| `CandleType` | Tipo de vela e período processados pela estratégia. |

## Notas de uso
- A estratégia é projetada para instrumentos cotados com um passo de preço fixo. As entradas baseadas em pontos são multiplicadas pelo `PriceStep` do instrumento para obter distâncias de preço.
- Dados do livro de ordens são usados para estimar o spread atual. Se as melhores cotações bid/ask não estiverem disponíveis, a filtragem de spread é ignorada.
- As saídas de trailing e timeout são avaliadas em velas fechadas, correspondendo à lógica MQL original.
- O dimensionamento de posição requer a valoração do portfólio (`Portfolio.CurrentValue`) quando `RiskModes` está configurado para porcentagem. Se o valor não estiver disponível, a estratégia volta ao tamanho de lote configurado.

## Arquivos
- `CS/V1n1LonnyBreakoutStrategy.cs` – Implementação da estratégia em C# para StockSharp.
- `README.md` – Esta descrição em inglês.
- `README_zh.md` – 中文简介。
- `README_ru.md` – Descrição em russo.
