# Estratégia Interceptor (Port StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Interceptor é um port em C# do consultor especializado original do MetaTrader5. Combina alinhamento de "leque" EMA multi-timeframe com osciladores Stochastic, detecção de rompimento de range plano, análise de divergência, filtros de vela martelo e confirmação de chifre (convergência do leque). O objetivo é explorar a continuação de tendência forte após períodos de consolidação no gráfico de 5 minutos do GBP/USD.

## Lógica central
- **Estrutura de tendência** – A estratégia avalia médias móveis exponenciais (comprimentos 34/55/89/144/233) nos períodos M5, M15 e H1. Uma tendência válida requer que todos os leques EMA estejam alinhados (ascendente para altista, descendente para baixista) e que a distância máxima entre o EMA mais lento e mais rápido permaneça abaixo de limiares configuráveis.
- **Confirmação de momentum** – Os osciladores Stochastic de M5 e M15 devem cruzar para fora de áreas de sobrecompra/sobrevenda para confirmar que o preço está saindo de zonas de congestão.
- **Filtro de rompimento de range plano** – Um detector de compressão de volatilidade procura ranges estreitos (comprimento e largura controlados por `FlatnessCoefficient`, `MinFlatBars` e `MaxFlatPoints`). Rompimentos dessas zonas adicionam confiança ao sinal.
- **Filtro de martelo** – Velas de martelo ou martelo invertido recentes (validadas por regras de corpo/sombra longa e máximos/mínimos locais) atuam como sinais de exaustão na direção do trade pretendido.
- **Verificação de divergência** – A estratégia procura divergências altistas/baixistas entre o preço e o oscilador Stochastic M5 para antecipar reversões após o alinhamento do leque.
- **Confirmação de chifre** – Quando o leque EMA M5 converge (o "chifre"), um rompimento acima/abaixo de um range recente desencadeia entradas adicionais se os períodos superiores suportarem o movimento.

## Condições de entrada
Um setup comprado pode ser acionado por uma ou múltiplas condições (cada uma adiciona peso à decisão):
1. Leques EMA alinhados nos três períodos, cruzamento altista do Stochastic M5, corpo de vela altista forte.
2. Vela de rompimento do leque EMA M5 que abre na mínima e fecha acima das EMAs rápidas.
3. Rompimento de range plano na direção altista.
4. Acordo de rompimento M5 + M15 enquanto as distâncias do leque EMA permanecem abaixo dos limiares permitidos.
5. Divergência altista entre Stochastic e preço enquanto os leques apontam para cima.
6. Vela de martelo altista recente dentro da janela de retrospectiva permitida.
7. Cruzamento altista Stochastic M15 com corpos de vela altistas.
8. Rompimento de chifre acima do range recente após convergência do leque EMA.

Os setups vendidos seguem a lógica espelhada. Se as condições compradas e vendidas estiverem simultaneamente presentes, a estratégia pula o trading para aquela barra.

## Saída e gestão de risco
- Stop-loss e take-profit fixos configuráveis em pontos.
- Lógica de breakeven opcional (`StopLossAfterBreakeven`, `TakeProfitAfterBreakeven`) que aperta o stop quando o preço atinge um limiar de lucro.
- Trailing stop baseado na distância do preço desde o último fechamento (`TrailingDistancePoints` com `TrailingStepPoints`).
- Quando uma nova posição é aberta, a estratégia fecha primeiro qualquer posição oposta existente.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Volume da ordem usado para cada entrada. |
| `FlatnessCoefficient` | Multiplicador que controla a largura máxima permitida de um range plano detectado. |
| `StopLossPoints` | Distância inicial do stop-loss em pontos de preço. |
| `TakeProfitPoints` | Distância inicial do take-profit em pontos de preço (0 desabilita). |
| `TakeProfitAfterBreakeven` | Lucro necessário (pontos) antes de a lógica de breakeven ser ativada. |
| `StopLossAfterBreakeven` | Distância do stop de breakeven após ativação. |
| `MaxFanDistanceM5/M15/H1` | Dispersão EMA máxima permitida em cada período. |
| `StochasticKPeriodM5/M15` | Comprimento %K para osciladores Stochastic em M5 e M15. |
| `StochasticUpperM5/M15` | Limiares de sobrecompra. |
| `StochasticLowerM5/M15` | Limiares de sobrevenda. |
| `MinBodyPoints` | Tamanho mínimo do corpo da vela para qualificar como barra forte. |
| `MinFlatBars` | Barras mínimas necessárias para definir um range plano. |
| `MaxFlatPoints` | Largura máxima do range plano (pontos). |
| `MinDivergenceBars` | Separação mínima entre pivôs de divergência. |
| `HammerLongShadowPercent` | Percentual mínimo de sombra longa para detecção de martelo. |
| `HammerShortShadowPercent` | Percentual máximo de sombra oposta para detecção de martelo. |
| `HammerMinSizePoints` | Alcance total mínimo da vela martelo. |
| `HammerLookbackBars` | Janela de retrospectiva para buscar padrões de martelo. |
| `HammerRangeBars` | Número de barras usadas para validar máximos/mínimos de martelo. |
| `MaxFanWidthAtNarrowest` | Dispersão EMA máxima quando o leque é considerado convergido. |
| `FanConvergedBars` | Número de barras que o leque pode permanecer convergido para sinais de chifre. |
| `RangeBreakLookback` | Janela de retrospectiva para detecção de rompimento de range. |
| `TrailingStepPoints` | Incremento mínimo para ajustes do trailing stop. |
| `TrailingDistancePoints` | Distância entre o preço e o trailing stop. |
| `CandleType` | Série de velas primária (padrão velas de tempo M5). |

## Notas de uso
- O consultor especializado original foi projetado para gráficos GBP/USD M5. Os parâmetros podem precisar de ajuste para outros instrumentos ou períodos.
- A estratégia requer a API de alto nível do StockSharp e dados de velas para intervalos M5, M15 e H1.
- Apenas uma posição líquida é mantida; posições opostas são fechadas antes de novos trades serem abertos.

## Aviso legal
A estratégia é fornecida para fins educacionais. O desempenho passado não garante resultados futuros. Sempre valide os parâmetros e a lógica em um ambiente de teste seguro antes de negociar com capital real.
