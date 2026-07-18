# Estratégia de linha dinâmica MoStAsHaR15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o especialista "MoStAsHaR15 FoReX - Pivot Line" MetaTrader 4 usando a estratégia de alto nível de StockSharp API. Ele mantém o mapa pivô diário original combinado com filtros de impulso de ADX, EMA spreads e o histograma MACD (OsMA). A lógica intradiária opera em um fluxo de velas de hora em hora, enquanto uma segunda assinatura consome a vela diária concluída anteriormente para reconstruir a escada dinâmica antes de cada decisão.

## Lógica de negociação
- **Cálculo do pivô** – a máxima, a mínima e o fechamento de ontem da série diária geram o pivô clássico (P), três níveis de resistência (R1–R3), três níveis de suporte (S1–S3) e seis pontos médios (M0–M5). O fechamento atual da vela é verificado em relação a esta escada para detectar a faixa circundante. O mapeamento incomum do EA original que liga a região entre M5 e R3 de volta ao segmento S3/M0 é preservado.
- **Filtro de distância** – as negociações são acionadas apenas quando a distância até o limite de lucro que limita o intervalo atual é maior que `MinimumDistancePips` (14 pips por padrão), espelhando as verificações originais de `dif1`/`dif2`.
- **Entradas longas** exigem todos os filtros a seguir:
  - A linha principal ADX está acima de `AdxThreshold` (20) e o componente +DI está subindo e é mais forte que −DI.
  - O EMA de base fechada está pelo menos `EmaSpreadPips` (5 pips) acima do EMA de base aberta, e a vela anterior já tinha a mesma ordem de alta.
  - O histograma MACD aumentou em comparação com a vela anterior (OsMA subindo).
- **Entradas curtas** espelham o ramo longo com força −DI, spread de baixa EMA e um histograma de queda MACD.
- Apenas uma posição líquida é permitida por vez. As ordens são enviadas com execução de mercado usando `BuyMarket()` e `SellMarket()`.

## Gerenciamento de posição
- **Stop-loss** – opcional, localizado `StopLossPips` abaixo/acima do preço de entrada. Defina como `0` para desativar como no EA original.
- **Take-profit** – fixado no limite do pivô (suporte ou resistência) que delimita a faixa de preço atual quando a negociação é aberta.
- **Trailing Stop** – quando o preço avança mais de `TrailingStopPips + TrailingStepPips` além da entrada, o stop é seguido para manter uma distância de `TrailingStopPips`. O valor do passo deve permanecer positivo sempre que o rastreamento estiver habilitado.
- Se o stop-loss, o trailing stop ou o take-profit forem tocados dentro de uma vela, a posição será fechada na avaliação dessa barra.

## Parâmetros de Estratégia
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `HourlyCandleType` | Série de velas intradiárias alimentando a lógica de execução. | 1 hora |
| `DailyCandleType` | Fluxo diário de velas usado para calcular os níveis de pivô. | 1 dia |
| `StopLossPips` | Distância inicial do stop-loss em pips. `0` desativa-o. | 20 |
| `TrailingStopPips` | Distância de parada final em pips. | 10 |
| `TrailingStepPips` | Movimento mínimo (em pips) antes das atualizações do trailing stop. Deve ser > 0 quando o rastreamento estiver ativado. | 5 |
| `MinimumDistancePips` | Distância mínima do pip até o limite do pivô próximo antes de entrar em uma negociação. | 14 |
| `EmaSpreadPips` | Spread necessário entre o fechamento EMA e a abertura EMA. | 5 |
| `AdxThreshold` | Leitura mínima de ADX que ativa o sinal. | 20 |
| `AdxPeriod` | Período do indicador ADX. | 14 |
| `EmaClosePeriod` | Comprimento EMA aplicado ao fechamento da vela. | 5 |
| `EmaOpenPeriod` | Comprimento EMA aplicado às aberturas da vela. | 8 |
| `MacdFastPeriod` | Período EMA rápido para MACD (numerador OsMA). | 12 |
| `MacdSlowPeriod` | Período EMA lenta para MACD. | 26 |
| `MacdSignalPeriod` | Período de sinal EMA para MACD. | 9 |

## Notas de conversão
- Os valores dos indicadores são avaliados apenas em velas finalizadas e nenhuma coleção contínua é armazenada – o estado é gerenciado por meio de campos escalares de acordo com as diretrizes do repositório.
- Pips são derivados da precisão `PriceStep` e decimal do título. Os símbolos citados com 3 ou 5 casas decimais usam a convenção "mini pip", assim como MetaTrader.
- O mapeamento de take-profit para a região M5→R3 recorre intencionalmente ao par S3/M0 para permanecer fiel ao código-fonte.
- Todos os comentários dentro da estratégia permanecem em inglês, conforme exigido pelas instruções do projeto.

## Dicas de uso
- Ajuste os tipos de velas para corresponder à sessão de negociação do seu instrumento, especialmente para mercados com rollovers diários fora do padrão.
- Como a lógica avalia paradas e metas em velas fechadas, pode ocorrer derrapagem adicional em comparação com a execução MetaTrader no nível do tick em mercados rápidos.
- Considere ajustar `MinimumDistancePips` e `EmaSpreadPips` ao aplicar a estratégia a ativos com diferentes regimes de volatilidade.
