# Estratégia de teste retangular
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Rectangle Test reproduz o especialista MetaTrader "RectangleTest" usando o StockSharp de alto nível de API. Ele detecta intervalos laterais em um período intradiário, verifica se duas médias móveis e o preço atual permanecem dentro do intervalo detectado e, em seguida, negocia rompimentos fora do retângulo na direção do EMA mais rápido. Toda a lógica é executada em velas concluídas recebidas de uma fonte de vela configurável.

## Lógica de negociação
1. Assine o fluxo de vela principal (padrão: período de 1 hora) e alimente-o nos seguintes indicadores:
   - **ExponentialMovingAverage (EMA)** com comprimento configurável `EmaPeriod`.
   - **SimpleMovingAverage (SMA)** com comprimento configurável `SmaPeriod`.
   - Indicadores **Mais alto** e **Mais baixo** com comprimento `RangeCandles`, configurados para ler máximos e mínimos de velas. Eles fornecem os limites do retângulo que emulam os cálculos baseados em matriz MetaTrader.
2. Depois que todos os indicadores estiverem formados, calcule a altura do retângulo em porcentagem em relação ao limite superior. Somente velas cuja altura é menor que `RectangleSizePercent` são consideradas consolidações válidas.
3. Exija que EMA, SMA e a vela próxima permaneçam dentro do retângulo. Isso reproduz o filtro lateral da versão MQL.
4. **Configuração curta**:
   - EMA está acima de SMA.
   - O preço de fechamento está acima de EMA (correspondendo à condição "Ask > EMA" de MetaTrader em velas concluídas).
   - A liquidação opcional de uma posição longa existente ocorre primeiro, após a qual uma ordem de mercado curta é enviada.
5. **Configuração longa**:
   - EMA está abaixo de SMA.
   - O preço de fechamento está abaixo de EMA (espelhando a regra "Lance < EMA").
   - As posições vendidas existentes são liquidadas antes da abertura da posição comprada.
6. Cada entrada registra o preço e o volume de entrada esperados. Quando a posição chega a zero, a estratégia compara o preço de saída com o preço de entrada armazenado. As negociações perdedoras aumentam o contador de perdas diárias, aplicando o filtro `MaxLosingTradesPerDay` exatamente como o auxiliar MQL `Loss()`.

## Gestão de dinheiro e risco
- A estratégia pode funcionar de dois modos:
  - **Modo baseado em risco** (`UseRiskMoneyManagement = true`): o volume da posição é dimensionado a partir do valor da conta, o `RiskPercent`, e do `StopLossPoints` configurado. O cálculo usa `Security.PriceStep`, `Security.StepPrice` e `Security.VolumeStep` para espelhar a rotina de dimensionamento de lote MetaTrader.
  - **Modo de volume fixo** (`UseRiskMoneyManagement = false`): as negociações usam o parâmetro `FixedVolume`.
- Após a posição líquida mudar de estável para diferente de zero, `SetStopLoss` e `SetTakeProfit` registram ordens de proteção usando `StopLossPoints` e `TakeProfitPoints` (expressas em etapas de preço), correspondendo às distâncias SL/TP passadas para `m_trade.Sell/Buy` no especialista original.
- `MaxLosingTradesPerDay` interrompe novos sinais pelo resto do dia assim que o número especificado de negociações perdidas for detectado.

## Gerenciamento de tempo
- A negociação é permitida apenas entre `TradeStartTime` e `TradeEndTime`. O auxiliar lida com intervalos que abrangem sessões da meia-noite e também diurnas.
- Quando `EnableTimeClose` for verdadeiro, todas as posições abertas serão liquidadas após `TimeClose`, replicando as entradas MetaTrader "TimeCloseTrue" e `TimeClose`.

## Diferenças vs. versão MetaTrader
- O indicador original criou retângulos gráficos no gráfico. StockSharp não cria objetos de desenho; em vez disso, o mesmo intervalo é calculado internamente através dos indicadores Mais Alto/Mais Baixo.
- As negociações perdedoras são contadas usando os preços de fechamento da vela sinalizadora. Isso corresponde à intenção de `Loss()` (contar pedidos perdidos por dia) enquanto permanece dentro de abstrações de StockSharp de alto nível.
- As características de preenchimento de pedidos, como `ORDER_FILLING_FOK/IOC`, são tratadas pelo ambiente de StockSharp, portanto, a configuração explícita do modo de preenchimento não é necessária.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `EmaPeriod` | 45 | Período do EMA rápida. |
| `SmaPeriod` | 200 | Período da lentidão SMA. |
| `RangeCandles` | 10 | Número de velas formando o retângulo. |
| `RectangleSizePercent` | 0,5 | Altura máxima do retângulo permitida para negociação. |
| `StopLossPoints` | 250 | Distância de stop-loss em etapas de preço. |
| `TakeProfitPoints` | 750 | Distância de lucro em etapas de preço. |
| `UseRiskMoneyManagement` | verdade | Alterne entre volume fixo e baseado em risco. |
| `RiskPercent` | 1 | Porcentagem do patrimônio da conta arriscado por negociação. |
| `FixedVolume` | 1 | Volume fixo quando o dimensionamento baseado em risco está desabilitado. |
| `MaxLosingTradesPerDay` | 1 | Limite diário de perdas em negociações. |
| `TradeStartTime` | 03:00 | Hora do dia em que as entradas são permitidas. |
| `TradeEndTime` | 22:50 | Hora do dia após a qual nenhuma nova entrada será gerada. |
| `EnableTimeClose` | falso | Permite a liquidação no final do dia. |
| `TimeClose` | 23:00 | Hora do dia para fechar todas as posições. |
| `CandleType` | Velas de 1 hora | Fonte de dados de vela primária. |

## Gráficos
Se uma área do gráfico estiver disponível, a estratégia desenha as velas de preço, rápidas EMA, lentas SMA e negociações próprias para visualizar os intervalos de intervalo e o tempo de negociação.
