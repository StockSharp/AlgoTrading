# Estratégia Starter V6 Mod (Conversão para StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia **Starter V6 Mod** é uma conversão para a API de alto nível do StockSharp do Expert Advisor do MetaTrader 5 `Starter_v6mod`. O sistema original combina um oscilador Laguerre RSI, duas médias móveis exponenciais duplas, um filtro de índice de canal de commodities e um módulo de gestão de posições em grade. Este port preserva a lógica de confirmação multicamada, adaptando o gerenciamento de posições, o gerenciamento de capital e as ações de proteção ao ambiente StockSharp.

## Lógica de trading

### Indicadores

* **Proxy Laguerre RSI** – modelado via RSI normalizado de 14 períodos para emular a escala 0-1 usada pelo oscilador Laguerre original. O par de níveis `LevelDown` / `LevelUp` (padrão 0,15 / 0,85) define zonas de sobrevendido e sobrecomprado.
* **EMA lenta (120)** e **EMA rápida (40)** – ambas calculadas no preço mediano da vela. Seu deslocamento relativo atua como filtro de direção de tendência. O parâmetro `AngleThreshold` converte a diferença de EMA em uma distância em ticks que condiciona as direções de trading.
* **Índice de Canal de Commodities (14)** – confirma a direção do momentum exigindo valores negativos para entradas compradas e positivos para entradas vendidas.

### Critérios de entrada

1. Determinar o viés de tendência a partir da diferença de EMA:
   * Se a EMA lenta menos a EMA rápida for menor que `-AngleThreshold` ticks, somente posições compradas podem ser iniciadas.
   * Se a diferença for maior que `AngleThreshold`, somente posições vendidas podem ser iniciadas.
   * Caso contrário, o mercado é considerado lateral e nenhuma nova posição é aberta.
2. Quando o viés de tendência permite uma direção, verificar os filtros de oscilador e momentum:
   * Configuração comprada – proxy Laguerre abaixo de `LevelDown`, EMA lenta < EMA lenta anterior, EMA rápida < EMA rápida anterior, e CCI < 0.
   * Configuração vendida – proxy Laguerre acima de `LevelUp`, EMA lenta > EMA lenta anterior, EMA rápida > EMA rápida anterior, e CCI > 0.
3. Espaçamento de grade – ao empilhar posições na mesma direção, o preço atual deve estar pelo menos `GridStepPips` abaixo da entrada comprada mais baixa ou acima da entrada vendida mais alta. Isso replica a lógica de médias do EA original.
4. Contagem de posições – o número total de entradas simultâneas em grade não pode exceder `MaxOpenTrades`.

### Critérios de saída

* **Saídas Laguerre** – posições compradas fecham quando o oscilador cruza acima de `LevelUp`; posições vendidas fecham quando cai abaixo de `LevelDown`.
* **Stop-loss / Take-profit** – expressos em pips, convertidos para incrementos de preço do instrumento. A conversão rastreia o ajuste original para símbolos com precificação de 3/5 casas decimais.
* **Trailing stop** – ativa após o preço avançar `(TrailingStopPips + TrailingStepPips)` e então segue o preço com um deslocamento de `TrailingStopPips`.
* **Proteções de sexta-feira** – nenhuma nova operação é permitida após as 18h00 (horário do terminal) e todas as posições abertas são liquidadas após as 20h00.

### Gestão de capital

* **Dimensionamento de volume** – fixo (`UseManualVolume = true`) ou baseado em risco. No modo de risco, o volume é igual a `(patrimônio * RiskPercent) / (distância StopLoss em unidades de preço)`.
* **Corte de patrimônio** – o trading para quando o patrimônio atual cai abaixo de `EquityCutoff`.
* **Limite de perdas diárias** – se a estratégia registrar `MaxLossesPerDay` saídas perdedoras na data atual, nenhuma posição adicional é aberta.
* **Recuperação de perdas** – após cada saída perdedora, o próximo tamanho de posição é dividido por `DecreaseFactor^perdasHoje`, espelhando a lógica de escalonamento de posições original.

## Notas de implementação

* A conversão usa o pipeline `SubscribeCandles().Bind(...)` de alto nível do StockSharp para transmitir velas terminadas e valores de indicadores para a lógica de decisão.
* O StockSharp não inclui um Laguerre RSI nativo, portanto, um RSI normalizado é usado como proxy. Os limiares correspondem ao intervalo 0-1 do Laguerre.
* O filtro de ângulo EMA é reproduzido medindo a diferença entre os valores de EMA lenta e rápida em ticks, fornecendo um portão direcional semelhante ao indicador personalizado `emaangle` original.
* O gerenciamento manual de stops e trailing é realizado dentro da rotina de processamento de velas para manter paridade com as modificações de trailing do MQL.
* A contabilidade da grade rastreia o preço médio de entrada, o preço de preenchimento mais baixo/alto e os níveis de trailing para emular o fluxo de trabalho multi-posição do MQL enquanto trabalha dentro do modelo de posição agregada do StockSharp.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `UseManualVolume` | `false` | Alternar entre dimensionamento de posição fixo e baseado em risco. |
| `ManualVolume` | `1` | Volume usado quando o dimensionamento manual está habilitado ou o baseado em risco não pode ser calculado. |
| `RiskPercent` | `5` | Percentual do patrimônio arriscado por operação quando o dimensionamento automático está ativo. |
| `StopLossPips` | `35` | Distância do stop-loss em pips. |
| `TakeProfitPips` | `10` | Distância do take-profit em pips. |
| `TrailingStopPips` | `0` | Distância do trailing stop em pips (0 desativa o trailing). |
| `TrailingStepPips` | `5` | Avanço mínimo antes do trailing stop começar a seguir o preço. |
| `DecreaseFactor` | `1.6` | Fator aplicado para reduzir o tamanho após cada perda. |
| `MaxLossesPerDay` | `3` | Máximo de saídas perdedoras permitidas por dia calendário. |
| `EquityCutoff` | `800` | Limite de patrimônio que interrompe novas operações. |
| `MaxOpenTrades` | `10` | Número máximo de entradas simultâneas em grade. |
| `GridStepPips` | `30` | Espaçamento mínimo entre entradas empilhadas na mesma direção. |
| `LongEmaPeriod` | `120` | Período do filtro EMA lento. |
| `ShortEmaPeriod` | `40` | Período do filtro EMA rápido. |
| `CciPeriod` | `14` | Período do Índice de Canal de Commodities. |
| `AngleThreshold` | `3` | Limiar de diferença de EMA expresso em ticks. |
| `LevelUp` | `0.85` | Nível superior de Laguerre. |
| `LevelDown` | `0.15` | Nível inferior de Laguerre. |
| `CandleType` | `15m` | Período de velas usado para os cálculos. |

## Dicas de uso

1. Configure o parâmetro `CandleType` para corresponder ao período usado na configuração original do MT5 (o EA é frequentemente implantado em gráficos de 15 minutos).
2. Alinhe as configurações de risco com as especificações da conta. Ao usar dimensionamento baseado em risco, certifique-se de que `StopLossPips` reflita a volatilidade do instrumento, pois afeta diretamente o volume calculado.
3. Revise os horários de trading da bolsa. A proteção de sexta-feira integrada assume que o relógio do servidor está alinhado com o fechamento de sessão desejado.
4. Habilite o desenho no gráfico (via `CreateChartArea`) para visualizar EMA, proxy RSI, CCI e operações executadas para depuração ou otimização.
5. Ao portar conjuntos de parâmetros de backtests do MT5, lembre-se de que o proxy RSI aproxima o oscilador Laguerre; pode ser necessário um ajuste fino dos limiares para corresponder ao timing de sinal original.

## Arquivos

* `CS/StarterV6ModStrategy.cs` – Implementação da estratégia StockSharp.
* `README.md` – Documentação em inglês (este arquivo).
* `README_zh.md` – Documentação em chinês simplificado.
* `README_ru.md` – Documentação em russo.
