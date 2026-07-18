# Estratégia Anubis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Anubis combina filtros de volatilidade e momentum em múltiplos períodos para capturar reversões contra picos de contratendência fortes. O consultor especializado original do MetaTrader 5 usava indicadores H4 para filtrar entradas e sinais M15 para o timing. Esta conversão mantém a mesma estrutura adaptando a lógica à API de alto nível do StockSharp e fornecendo telemetria de execução detalhada.

## Lógica da estratégia
- **Períodos**
  - Período do sinal principal: tipo de vela configurável (velas de 15 minutos por padrão).
  - Confirmação do período superior: velas fixas de 4 horas usadas para CCI e desvios padrão.
- **Indicadores**
  - *Commodity Channel Index (CCI)* no período superior detecta extremos de sobrecompra/sobrevenda.
  - *Dois desvios padrão* no período superior fornecem medições de volatilidade para dimensionamento do take-profit.
  - *MACD* no período do sinal fornece confirmação de cruzamento de momentum.
  - *Average True Range (ATR)* no período do sinal define saídas por amplitude de vela anormal.
- **Critérios de entrada**
  - **Comprado:** CCI cai abaixo de `-CciThreshold`, a linha principal do MACD cruza acima da linha de sinal, e o histograma MACD anterior era negativo.
  - **Vendido:** CCI sobe acima de `+CciThreshold`, a linha principal do MACD cruza abaixo da linha de sinal, e o histograma MACD anterior era positivo.
  - A estratégia fecha opcionalmente uma posição oposta antes de empilhar uma nova e impõe um espaçamento mínimo de preço entre entradas consecutivas.
- **Gestão de posição**
  - Até `MaxLongPositions` ou `MaxShortPositions` entradas empilhadas são permitidas, cada uma aberta com `TradeVolume` contratos.
  - As distâncias de stop-loss e take-profit são derivadas de configurações baseadas em pips e da volatilidade do período superior.
  - Uma vez que o preço se mova `BreakevenPips`, o stop protetor é elevado ao preço médio de entrada.
- **Critérios de saída**
  - Stops fixos: os níveis de stop-loss e take-profit são monitorados em cada vela fechada.
  - Saídas por amplitude: posições fecham se a amplitude da vela anterior exceder `CloseAtrMultiplier × ATR`.
  - Saídas por momentum: posições com lucro suficiente fecham quando o momentum do MACD vira contra a operação e o ganho excede `ThresholdPips`.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | 1 | Tamanho da ordem para cada entrada. |
| `CciThreshold` | 80 | Nível absoluto de CCI no gráfico de 4 horas usado para detectar extremos. |
| `CciPeriod` | 11 | Comprimento de retrocesso do CCI no período superior. |
| `StopLossPips` | 100 | Distância de stop-loss expressa em pips. Defina como 0 para desabilitar o stop inicial. |
| `BreakevenPips` | 65 | Distância de lucro em pips antes de mover o stop para o ponto de equilíbrio. |
| `ThresholdPips` | 28 | Margem de lucro adicional necessária antes de acionar as saídas baseadas em MACD. |
| `TakeStdMultiplier` | 2.9 | Multiplicador aplicado ao desvio padrão lento ao calcular a distância de take-profit. |
| `CloseAtrMultiplier` | 2 | Multiplicador do ATR do período do sinal usado para saídas baseadas em amplitude. |
| `SpacingPips` | 20 | Distância mínima de preço entre entradas consecutivas na mesma direção. |
| `MaxLongPositions` | 2 | Número máximo de entradas compradas simultâneas. |
| `MaxShortPositions` | 2 | Número máximo de entradas vendidas simultâneas. |
| `MacdFastLength` | 20 | Comprimento da EMA rápida para MACD no período do sinal. |
| `MacdSlowLength` | 50 | Comprimento da EMA lenta para MACD no período do sinal. |
| `MacdSignalLength` | 2 | Comprimento de suavização do sinal para MACD. |
| `AtrLength` | 12 | Período de retrocesso do ATR no período do sinal. |
| `StdFastLength` | 20 | Período para o desvio padrão rápido (usado para diagnósticos). |
| `StdSlowLength` | 30 | Período para o desvio padrão lento que orienta a distância de take-profit. |
| `CandleType` | Velas de 15m | Período principal usado para cálculos de MACD e ATR. |

## Notas de trading
- O período superior está fixo em quatro horas; ajuste `CandleType` se desejar sincronizar o período do sinal principal com mercados diferentes.
- Como o StockSharp agrega posições netas por padrão, exposições compradas e vendidas não são mantidas simultaneamente; um sinal oposto zerará a posição aberta antes de colocar a nova ordem.
- O cálculo do desvio padrão segue a implementação do StockSharp. O comprimento lento aproxima o desvio baseado em EMA da versão MQL original.
- Certifique-se de que o instrumento selecionado expõe um `PriceStep` válido para que parâmetros baseados em pips sejam traduzidos com precisão em distâncias de preço.
