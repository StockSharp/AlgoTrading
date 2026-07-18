# Estratégia MostasHaR15 Pivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o comportamento do Consultor Especialista original **MostasHaR15 Pivot** MQL5 usando a API de alto nível do StockSharp. Combina cálculos clássicos de pivô diário de suelo com filtros de momentum do ADX, diferenciais de EMA e o histograma MACD (OsMA). A estratégia opera em um fluxo de velas intradía (1 hora por padrão) e consome a vela diária completada anterior para reconstruir o mapa de pivô em cada barra.

## Lógica de negociação
- **Grade de pivô** – os máximos, mínimos e fechamentos diários anteriores são usados para calcular o pivô principal (P), três níveis de resistência (R1–R3), três níveis de suporte (S1–S3) e seis pontos médios (M0–M5). O fechamento da vela atual é comparado com essa escada para identificar o segmento de suporte e resistência circundante. Um caso especial herdado do EA mapeia preços entre M5 e R3 de volta ao intervalo S3/M0.
- **Filtro de distância** – as negociações são permitidas apenas quando a distância para o nível de take-profit mais próximo é maior que `MinimumDistancePips` (14 pips por padrão), que corresponde aos filtros originais `dif1`/`dif2`.
- **Entradas compradas** requerem tudo o seguinte:
  - A linha principal ADX excede `AdxThreshold` (20) e o +DI está tanto subindo quanto acima do –DI.
  - A EMA de 5 períodos nos fechamentos de velas está pelo menos `EmaSlopePips` (5 pips) acima da EMA de 8 períodos nas aberturas de velas, e a barra anterior mostrou o mesmo ordenamento EMA de alta.
  - O histograma MACD (OsMA) aumentou em comparação com a barra anterior.
- **Entradas vendidas** espelham as condições compradas com força –DI, spread EMA baixista e um histograma MACD caindo.
- Apenas uma posição líquida é permitida. As ordens são colocadas com execução de mercado via `BuyMarket()`/`SellMarket()`.

## Gestão de posição
- **Stop-loss** – opcional, localizado `StopLossPips` abaixo/acima do preço de entrada. Definir o parâmetro como `0` desativa o stop inicial, como no EA.
- **Take-profit** – fixo na fronteira de pivô mais próxima que envolve o preço atual quando a posição é aberta.
- **Stop trailing** – replica a lógica de trailing original. Uma vez que o preço avança mais de `TrailingStopPips + TrailingStepPips` desde a entrada, o stop é movido para manter uma distância de trailing de `TrailingStopPips`. O trailing pode ser desativado definindo `TrailingStopPips` como `0`.
- Se o stop-loss, trailing stop ou take-profit for atingido durante uma vela, a posição é encerrada no fechamento dessa vela.

## Parâmetros da estratégia
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Série de velas intradía usada para negociação. | Período de 1 hora |
| `DailyCandleType` | Série de velas diárias para cálculos de pivô. | Período de 1 dia |
| `StopLossPips` | Distância do stop-loss em pips. Definir `0` para desativar. | 20 |
| `TrailingStopPips` | Distância do stop trailing em pips. | 5 |
| `TrailingStepPips` | Movimento mínimo favorável antes que o trailing seja atualizado. Deve ser >0 se o trailing estiver habilitado. | 5 |
| `MinimumDistancePips` | Distância mínima em pips para a fronteira de pivô mais próxima antes de entrar em uma negociação. | 14 |
| `EmaSlopePips` | Separação necessária entre a EMA de fechamento e a EMA de abertura. | 5 |
| `AdxThreshold` | Leitura mínima de ADX para negociações compradas e vendidas. | 20 |
| `AdxPeriod` | Comprimento do indicador ADX. | 14 |
| `EmaClosePeriod` | Período EMA aplicado aos fechamentos de velas. | 5 |
| `EmaOpenPeriod` | Período EMA aplicado às aberturas de velas. | 8 |
| `MacdFastPeriod` | Período EMA rápida dentro do histograma MACD. | 12 |
| `MacdSlowPeriod` | Período EMA lenta dentro do histograma MACD. | 26 |
| `MacdSignalPeriod` | Período EMA de sinal dentro do histograma MACD. | 9 |

## Notas de conversão
- A estratégia mantém o comportamento incomum do EA onde o intervalo de preços entre o nível médio M5 e a resistência R3 é mapeado de volta ao par de suporte/resistência S3/M0.
- Todos os valores de indicadores são processados apenas em velas completadas. Nenhuma coleção histórica é armazenada; todo o estado é mantido em campos escalares conforme as diretrizes do repositório.
- Os comentários na estratégia permanecem em inglês por instrução do repositório.

## Dicas de uso
- Ajuste `CandleType` e `DailyCandleType` ao aplicar a estratégia a mercados com diferentes sessões de negociação.
- Como a lógica de stop-loss e trailing é avaliada em velas fechadas, pode aparecer slippage adicional em mercados rápidos em comparação com a execução no nível de tick no EA original.
