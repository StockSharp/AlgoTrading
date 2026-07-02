# Estratégia de gerenciamento de risco ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de gerenciamento de riscos ATR é uma conversão StockSharp do MetaTrader 5 especialistas *Gerenciamento de riscos EA baseado em ATR volatilidade*. O EA original se concentrava no dimensionamento automático das posições de acordo com o saldo da conta e a volatilidade atual do mercado medida pelo Average True Range (ATR). A porta StockSharp mantém a mesma filosofia: ela só abre posições longas quando uma média móvel simples de 10 períodos cruza acima de uma média móvel simples de 20 períodos, e cada tamanho de entrada é calculado de modo que a perda potencial no stop de proteção corresponda à porcentagem de risco configurada.

A conversão segue o StockSharp API de alto nível. Os cálculos do indicador dependem dos componentes `AverageTrueRange` e `SimpleMovingAverage` anexados à assinatura da vela, em vez de chamadas diretas do indicador. O gerenciamento comercial reutiliza StockSharp métodos auxiliares, cancelando e recriando o stop de proteção após cada preenchimento para que a posição líquida e a ordem de stop sempre correspondam.

## Lógica de negociação
1. Assine o prazo definido por `CandleType` e aguarde as velas totalmente fechadas para evitar decisões prematuras.
2. Alimente um ATR de 14 períodos e duas médias móveis simples (comprimentos 10 e 20) com os dados de assinatura.
3. Quando a média móvel rápida fechar acima da média móvel lenta e não houver posição aberta, calcule o tamanho da posição com base no modelo de risco selecionado e envie uma ordem de compra de mercado.
4. Após cada preenchimento, calcule a distância de stop-loss: `ATR * AtrMultiplier` ou um número fixo de etapas de preço quando `UseAtrStopLoss` estiver desativado.
5. Arredonde o preço stop para o tick mais próximo e coloque uma ordem `SellStop` com o tamanho da posição atual. Qualquer parada anterior é cancelada antes que a nova seja registrada.
6. Quando a ordem de parada é executada e a posição retorna a zero, a estratégia limpa seu estado interno, pronta para o próximo cruzamento.

## Gestão de risco
- `RiskPercentage` determina quanto do valor do portfólio pode ser perdido em uma única negociação. A estratégia lê `Portfolio.CurrentValue` (ou `BeginValue` como alternativa) e multiplica-o pela percentagem para obter o risco monetário permitido.
- O risco permitido é dividido pela distância do stop loss para obter o volume de negociação. O arredondamento de volume respeita a etapa de volume do instrumento, as restrições mínimas e máximas para que as ordens geradas permaneçam válidas na bolsa.
- Se `RiskPercentage` for definido como `0`, a estratégia volta para a propriedade padrão `Volume` (1 lote por padrão) enquanto mantém a parada de proteção automática.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Série de velas primárias processadas pela estratégia. |
| `AtrPeriod` | `int` | `14` | Número de velas usadas para suavizar o indicador ATR. |
| `AtrMultiplier` | `decimal` | `2.0` | Multiplicador aplicado ao valor ATR para derivar a distância do stop-loss. |
| `RiskPercentage` | `decimal` | `1.0` | Porcentagem do valor da carteira arriscada em cada negociação. Defina como zero para usar um volume fixo. |
| `UseAtrStopLoss` | `bool` | `true` | Quando ativado a parada é colocada em `ATR * AtrMultiplier`; caso contrário, uma distância fixa será usada. |
| `FixedStopLossPoints` | `int` | `50` | Número de etapas de preço usadas para o stop protetor sempre que o posicionamento baseado em ATR é desativado. |

## Diferenças do original EA
- StockSharp trabalha com posições líquidas, portanto a conversão apenas envia ordens de compra a mercado. As saídas acontecem através do protetor `SellStop`, que reproduz o comportamento EA de estar sempre flat após uma parada.
- MetaTrader expõe a constante `_Point` para o tamanho do tick. A porta consulta `Security.PriceStep` e retorna para uma unidade monetária única quando o instrumento não fornece uma especificação de tick.
- O dimensionamento de posição respeita os filtros de volume de StockSharp (`VolumeStep`, `MinVolume`, `MaxVolume`) para garantir que a carteira de pedidos aceite os tamanhos de pedidos gerados.
- O processamento do indicador é orientado por eventos por meio de `Subscription.Bind(...)` em vez de chamadas síncronas `iMA`/`iATR`.

## Dicas de uso
- Certifique-se de que o portfólio conectado relate um `CurrentValue` correto; caso contrário, o dimensionamento da posição com base no risco voltará ao volume zero.
- A propriedade `Volume` ainda atua como uma rede de segurança. Se você deseja um tamanho de lote fixo, independentemente dos cálculos de ATR, defina `RiskPercentage` como zero e ajuste `Volume` antes de iniciar a estratégia.
- Anexe a estratégia a um gráfico para visualizar as velas, tanto as médias móveis quanto as negociações executadas. Isso ajuda a confirmar que novas entradas só aparecem quando a média rápida fecha acima da lenta e que as paradas ficam exatamente abaixo da última oscilação de preço.
- Considere aumentar `AtrMultiplier` em instrumentos mais voláteis para evitar interrupções prematuras ou desative o posicionamento baseado em ATR e forneça uma distância fixa personalizada por meio de `FixedStopLossPoints`.

## Indicadores
- `AverageTrueRange` (comprimento `AtrPeriod`).
- `SimpleMovingAverage` (comprimento rápido `10`).
- `SimpleMovingAverage` (comprimento lento `20`).
