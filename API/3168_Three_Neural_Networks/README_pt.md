# Estratégia de Three Neural Networks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port de alto nível do StockSharp do assessor especialista do MetaTrader "Three neural networks". Funciona inteiramente através da API de subscrição de velas do StockSharp e reutiliza indicadores `SmoothedMovingAverage` integrados para emular as três camadas neurais da implementação original. A estratégia opera em três períodos diferentes (H1, H4, D1) e analisa a inclinação de cada média suavizada para derivar uma decisão de negociação coletiva.

## Fluxo de trabalho

1. Quando a estratégia começa, ela subscreve velas dos períodos H1, H4 e D1 e vincula médias móvias suavizadas que usam o preço mediano, espelhando as chamadas `iMA(..., MODE_SMMA, PRICE_MEDIAN)` do MetaTrader.
2. Cada período mantém um histórico contínuo que respeita o deslocamento configurado. Assim que quatro valores deslocados estão disponíveis, o algoritmo calcula três saídas neurais usando exatamente a mesma fórmula de diferença ponderada do EA e arredonda o resultado para quatro casas decimais.
3. Após o fechamento da vela H1, a estratégia combina as saídas neurais:
   - Se todos os três valores são positivos → abrir ou manter uma posição comprada.
   - Se a saída H1 é positiva enquanto as saídas H4 e D1 são negativas → abrir ou manter uma posição vendida.
4. Os posições são dimensionadas com um lote fixo ou um modelo de porcentagem de risco. No modo de risco, a estratégia aloca `VolumeOrRisk` por cento do valor do portfólio e o converte em volume dividindo pelo preço atual.
5. A lógica protetora replica os controles do EA: um stop-loss e take-profit são colocados em variáveis locais imediatamente após a mudança de direção do posição, e um Trailing stop é ajustado cada vez que a barra H1 fecha se o preço avança além da distância de trailing mais o passo configurado.
6. Cada vela H1 concluída primeiro verifica se os níveis atuais de stop-loss ou take-profit são violados e fecha a posição com uma ordem de mercado se necessário. O registro detalhado opcional reproduz o sinalizador `InpPrintLog` original.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StopLossPips` | `50` | Distância de stop de proteção em pips. Defina como `0` para desativar o stop-loss. |
| `TakeProfitPips` | `50` | Distância de take-profit em pips. Defina como `0` para desativar o objetivo. |
| `TrailingStopPips` | `15` | Distância entre o preço atual e o Trailing stop. |
| `TrailingStepPips` | `5` | Melhoria mínima necessária antes de mover o Trailing stop novamente. |
| `ManagementMode` | `RiskPercent` | Modo de dimensionamento de volume. `FixedLot` usa o valor como tamanho de lote direto; `RiskPercent` como porcentagem do patrimônio do portfólio. |
| `VolumeOrRisk` | `1` | Tamanho de lote ou porcentagem de risco, dependendo do modo de gestão de dinheiro. |
| `H1Period`, `H1Shift` | `2`, `5` | Período e deslocamento da média móvel suavizada H1. |
| `H4Period`, `H4Shift` | `2`, `5` | Período e deslocamento da média móvel suavizada H4. |
| `D1Period`, `D1Shift` | `2`, `5` | Período e deslocamento da média móvel suavizada D1. |
| `P1`, `P2`, `P3` | `0.1` | Pesos aplicados aos três componentes neurais H1. |
| `Q1`, `Q2`, `Q3` | `0.1` | Pesos aplicados aos três componentes neurais H4. |
| `K1`, `K2`, `K3` | `0.1` | Pesos aplicados aos três componentes neurais D1. |
| `EnableDetailedLog` | `false` | Ativa mensagens de diagnóstico detalhadas que espelham a saída de registro do EA. |

## Gestão de risco

- Os níveis de stop-loss e take-profit são traduzidos de distâncias em pips usando o tamanho de pip detectado (com ajuste automático de 3/5 dígitos idêntico ao código original) e aplicados imediatamente após a mudança de direção da posição.
- A lógica de trailing segue as condições do MetaTrader: torna-se ativa assim que o preço se move mais de `TrailingStopPips + TrailingStepPips` do ponto de entrada e avança apenas se a melhoria exceder o passo configurado.
- Todas as saídas são executadas com ordens de mercado `ClosePosition()` porque ordens stop/limite do lado do servidor não estão disponíveis na API de alto nível.

## Notas

- A validação de nível de congelamento/stop do EA não está disponível no StockSharp, portanto a estratégia depende apenas da conversão de tamanho de pip e normalização de volume através de `VolumeStep`, `VolumeMin` e `VolumeMax`.
- O dimensionamento baseado em risco usa o valor atual do portfólio e o preço de entrada para aproximar a verificação de margem do MetaTrader. Isso espelha o comportamento geral sem depender de calculadoras de margem específicas do corretor.
- O registro opcional pode ser habilitado através de `EnableDetailedLog` para diagnósticos passo a passo semelhantes a `InpPrintLog` no MetaTrader.
