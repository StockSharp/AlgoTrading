# Estratégia de Risk Reward Ratio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Risk Reward Ratio** é um port de alto nível do StockSharp do especialista MetaTrader "Risk Reward Ratio". A estratégia combina vários filtros de confirmação de momentum e tendência com um módulo disciplinado de gestão de risco. As entradas são geradas a partir da confluência de osciladores estocásticos, um cruzamento de média móvel ponderada linear (LWMA), um filtro RSI de 14 períodos e uma verificação de tendência MACD. O controle de risco é alcançado através de um stop-loss baseado em pips, um take-profit de ratio de recompensa automático, stops de seguimento e lógica de break-even opcionais, e um interruptor de saída de emergência que liquida imediatamente a posição.

A conversão mantém o espírito original do especialista MetaTrader usando as subscrições de velas e APIs de binding de indicadores do StockSharp. Todo o processamento de indicadores ocorre em velas concluídas e evita o acesso direto aos buffers de indicadores, preservando o paradigma de streaming do motor.

## Lógica de trading
1. **Confluência estocástica**
   * Um estocástico *rápido* (5, 2, 2) fornece o sinal primário de momentum usando a linha %K.
   * Um estocástico *lento* (21, 10, 4) fornece o viés direcional através da sua linha %D suavizada.
   * Setups longos requerem que o %K rápido esteja acima do %D lento, enquanto setups curtos requerem o oposto.
2. **Confirmação RSI**
   * Um RSI de 14 períodos deve estar acima de 50 para negociações longas e abaixo de 50 para curtas, garantindo que o mercado esteja alinhado com a direção proposta.
3. **Filtro de tendência via LWMAs**
   * Duas médias móveis ponderadas linealmente (comprimentos 6 e 85) devem confirmar a direção: LWMA rápida acima da lenta para comprados e abaixo para vendidos.
4. **Qualificador de tendência MACD**
   * O histograma MACD (12, 26, 9) deve estar de acordo com a direção do sinal. A linha principal deve liderar a linha de sinal permanecendo no lado apropriado do zero.
5. **Filtro de desvio de momentum**
   * Um indicador de Momentum de 14 períodos mede a distância a partir de 100. Pelo menos uma das últimas três leituras de momentum deve exceder o limiar configurável para provar que o preço está acelerando o suficiente para justificar uma negociação.
6. **Limites de posição**
   * A exposição líquida é limitada por `MaxPositions * TradeVolume`, de modo que a estratégia não possa piramidizar além da restrição original do EA.

As ordens são enviadas como execuções de mercado usando `BuyMarket` e `SellMarket`. A estratégia ignora velas não concluídas e mantém todo o estado dentro de campos de classe para respeitar a arquitetura orientada a eventos do StockSharp.

## Gestão de risco
* **Stop-loss em pips** – Cada entrada instala um stop de proteção a `StopLossPips * PriceStep` do preço de preenchimento.
* **Take-profit com ratio de recompensa** – A distância do take-profit é igual à distância do stop multiplicada por `RewardRatio` para manter uma relação fixa de recompensa-risco.
* **Trailing stop** – Quando habilitado, o stop se move atrás do preço por `TrailingStopPips` assim que o mercado avança pelo menos essa distância da entrada.
* **Ajuste de break-even** – Após `BreakEvenTriggerPips` de movimento favorável, o stop é movido para a entrada mais uma almofada adicional de `BreakEvenOffsetPips` (ou menos para vendidos), assegurando ganhos.
* **Interruptor de saída** – Definir `ExitSwitch` como `true` achata a posição atual na próxima vela concluída e desabilita o processamento até que o sinalizador seja desativado.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume de cada ordem de mercado. |
| `CandleType` | Período de `15m` | Série de velas principal. |
| `FastMaPeriod` | `6` | Período da LWMA rápida. |
| `SlowMaPeriod` | `85` | Período da LWMA lenta. |
| `MomentumThreshold` | `0.3` | Distância absoluta mínima do indicador de Momentum a partir de 100 necessária para permitir entradas. |
| `RewardRatio` | `2` | Múltiplo de take-profit relativo ao stop-loss. |
| `StopLossPips` | `20` | Distância do stop-loss em pips (múltiplos de PriceStep). |
| `MaxPositions` | `10` | Número máximo de unidades de volume (`TradeVolume`) permitidas simultaneamente. |
| `EnableTrailing` | `true` | Habilita atualizações de trailing stop baseadas em pips. |
| `TrailingStopPips` | `40` | Distância do trailing stop em pips. |
| `EnableBreakEven` | `true` | Ativa o gerenciamento de stop de break-even. |
| `BreakEvenTriggerPips` | `30` | Lucro (em pips) necessário antes de mover o stop para break-even. |
| `BreakEvenOffsetPips` | `30` | Deslocamento adicional em pips quando o stop se move para break-even. |
| `ExitSwitch` | `false` | Força a estratégia a fechar toda a exposição na próxima vela concluída. |

## Fluxo de trabalho
1. Configure o instrumento e a série de velas desejados, depois defina os parâmetros de risco.
2. Inicie a estratégia. Ela se subscreve às velas, vincula indicadores e começa a processar barras concluídas.
3. Quando as condições de entrada se alinham, o motor envia uma ordem de mercado e armazena os níveis de stop/alvo.
4. Em cada vela concluída, o bloco de risco avalia as regras de trailing, break-even e saída de emergência.
5. As saídas são acionadas ao atingir níveis de stop/take-profit, atualizações de trailing, ajustes de break-even ou o interruptor de emergência.

## Notas
* A conversão aproveita o binding de indicadores do StockSharp em vez do acesso manual a buffers, garantindo que cada indicador receba dados sincronizados.
* Todos os cálculos dependem do `PriceStep` do instrumento. Se o passo for zero ou estiver ausente, as distâncias de risco permanecem desabilitadas para evitar níveis de preço inválidos.
* A estratégia não modifica ordens pendentes; simplesmente envia ordens de mercado para abrir/fechar posições, espelhando a forma como o EA original achatava a exposição quando os limiares eram atingidos.
