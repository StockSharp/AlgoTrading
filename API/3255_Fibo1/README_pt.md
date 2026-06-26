# Estratégia de FIBO1 (Conversão MQL 24845)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia FIBO1** reproduz as regras de trading do consultor especialista original `FIBO1.mq4` de Aharon Tzadik (script MQL 24845) usando a API de alto nível do StockSharp. A estratégia negocia um único símbolo em um período selecionado e combina três grupos de filtros:

1. **Filtro de tendência** – uma LWMA rápida e uma lenta (Média Móvel Ponderada Linearmente) do preço típico. Sinais longos requerem que a LWMA rápida permaneça acima da LWMA lenta, enquanto os curtos requerem a relação inversa.
2. **Confirmação de Momentum** – três leituras de Momentum consecutivas são comparadas contra limiares de compra/venda definidos pelo usuário. O algoritmo imita o desvio absoluto de 100 que o código MQL usava em períodos superiores.
3. **Filtro MACD** – um MACD de período superior deve confirmar a direção do trade. O porte StockSharp mantém os padrões 12/26/9 e verifica a relação entre as linhas principal e de sinal do MACD exatamente como no consultor especialista.

Uma vez que uma posição está ativa, a estratégia recria a sofisticada lógica de saída do `FIBO1.mq4`:

- Distâncias tradicionais de stop-loss e take-profit baseadas em pips.
- Alvos opcionais de take-profit e trailing baseados em dinheiro/percentual.
- Trailing stops baseados em candles que seguem máximas/mínimas recentes, incluindo um buffer de preço adicional idêntico à configuração "PAD AMOUNT".
- Distâncias de trailing clássicas que ativam após um limiar mínimo de lucro.
- Proteção automática de ponto de equilíbrio com um offset expresso em pips.
- Um stop de capital que monitora o drawdown flutuante contra o pico histórico de capital.

> **Nota:** O especialista MQL original dependia de uma linha "FIBO" desenhada manualmente no gráfico para trading ao vivo. As estratégias StockSharp não podem acessar objetos de desenho do terminal, portanto o porte sempre se comporta como o ramo de teste do código MQL (a parte que ignora o filtro de retração Fib). Todos os outros recursos são preservados.

## Lógica de trading

1. **Detecção de sinal**
   - Aguardar um candle terminado no período principal.
   - Garantir que a LWMA rápida esteja acima (longo) ou abaixo (curto) da LWMA lenta.
   - Verificar o padrão de preço que compara o par de máxima/mínima do candle anterior, espelhando `Low[2] < High[1]` para longos e `Low[1] < High[2]` para curtos.
   - Avaliar o desvio absoluto máximo dos últimos três valores de Momentum do nível neutro 100. Se exceder o limiar configurado, o filtro de Momentum passa.
   - Confirmar que a linha principal MACD do período superior permaneça acima (longo) ou abaixo (curto) de sua linha de sinal.
   - Quando todos os filtros se alinham, reverter qualquer exposição oposta e abrir uma ordem de mercado usando o volume de trade configurado.

2. **Gestão de risco**
   - Cada nova posição recebe imediatamente ordens de stop-loss e take-profit baseadas em pips através da API protetora do StockSharp.
   - A lógica de ponto de equilíbrio ajusta o stop quando o lucro flutuante iguala o limiar de ativação.
   - O trailing baseado em preço pode operar em dois modos: (a) seguir extremos de candles com um offset de preenchimento, ou (b) manter uma distância fixa em pips após o trade entrar em lucro.
   - Um módulo de gestão de dinheiro lida com alvos baseados em dinheiro, alvos de percentual de capital e um trailing stop de lucro flutuante idêntico ao EA original.
   - O stop de capital global rastreia continuamente o nível de capital mais alto observado desde o início e fecha todas as posições quando o drawdown máximo permitido é ultrapassado.

## Parâmetros

| Nome | Padrão | Descrição |
|------|---------|-------------|
| `UseMoneyTakeProfit` | `false` | Fechar todas as posições quando o lucro não realizado atingir `MoneyTakeProfit` (moeda da conta). |
| `MoneyTakeProfit` | `10` | Meta de lucro em moeda da conta. Efetivo apenas se `UseMoneyTakeProfit = true`. |
| `UsePercentTakeProfit` | `false` | Habilitar uma meta de lucro expressa como percentual do snapshot inicial de capital. |
| `PercentTakeProfit` | `10` | Percentual usado pela meta de lucro baseada em capital. |
| `EnableMoneyTrailing` | `true` | Ativa trailing baseado em dinheiro quando o lucro não realizado atinge `MoneyTrailTarget`. |
| `MoneyTrailTarget` | `40` | Lucro flutuante mínimo que habilita a lógica de trailing monetário. |
| `MoneyTrailStop` | `10` | Drawdown máximo permissível (em unidades de moeda) após o trailing monetário ser ativado. |
| `UseEquityStop` | `true` | Habilitar proteção global contra drawdown de capital. |
| `EquityRiskPercent` | `1` | Drawdown máximo (percentual do capital pico) antes de fechar todas as posições. |
| `TradeVolume` | `1` | Volume base (lotes/contratos) para entradas de mercado. |
| `FastMaPeriod` | `20` | Período da LWMA rápida calculada sobre o preço típico. |
| `SlowMaPeriod` | `100` | Período da LWMA lenta calculada sobre o preço típico. |
| `MomentumPeriod` | `14` | Comprimento do indicador Momentum para o filtro de confirmação. |
| `MomentumBuyThreshold` | `0.3` | Desvio absoluto mínimo de 100 necessário para trades longos. |
| `MomentumSellThreshold` | `0.3` | Desvio absoluto mínimo de 100 necessário para trades curtos. |
| `MacdFastPeriod` | `12` | Comprimento EMA rápida dentro do MACD de período superior. |
| `MacdSlowPeriod` | `26` | Comprimento EMA lenta dentro do MACD de período superior. |
| `MacdSignalPeriod` | `9` | Comprimento EMA de sinal dentro do MACD de período superior. |
| `TakeProfitPips` | `50` | Distância de take-profit de proteção em pips. |
| `StopLossPips` | `20` | Distância de stop-loss de proteção em pips. |
| `TrailingActivationPips` | `40` | Lucro mínimo (pips) antes do trailing baseado em pips ser ativado. |
| `TrailingDistancePips` | `40` | Distância mantida pelo trailing stop baseado em preço. |
| `UseCandleTrailing` | `true` | Quando habilitado, o trailing stop segue extremos de candles recentes em vez de usar distância fixa. |
| `CandleTrailingLength` | `3` | Número de candles terminados para calcular o extremo do trailing. |
| `CandleTrailingOffsetPips` | `3` | Buffer de pips adicional no preço do trailing de candles. |
| `MoveToBreakEven` | `true` | Habilitar proteção de ponto de equilíbrio. |
| `BreakEvenActivationPips` | `30` | Lucro (pips) antes de o stop mover para o ponto de equilíbrio. |
| `BreakEvenOffsetPips` | `30` | Offset (pips) além do preço de entrada quando o ponto de equilíbrio é aplicado. |
| `CandleType` | `15m` | Série de candles principal para sinais de trading. |
| `MomentumCandleType` | `15m` | Série de candles para o indicador Momentum. |
| `MacdCandleType` | `1d` | Série de período superior para o filtro MACD. |

## Notas de uso

- Os tipos de candle padrão espelham a lógica multi-período do consultor especialista: as séries principal e de Momentum usam o período do gráfico, enquanto o MACD trabalha em um período superior (diário por padrão). Todas as três séries podem ser reconfiguradas.
- A rotina de conversão de pips leva em conta automaticamente símbolos forex de 3/5 decimais multiplicando o passo de preço por 10. Instrumentos com outros tamanhos de tick mantêm o multiplicador `PriceStep` bruto.
- A estratégia depende exclusivamente de candles terminados. Certifique-se de que o provedor de dados conectado publique estados de candle, caso contrário as condições de entrada nunca serão acionadas.
- Quando o símbolo opera em um ambiente de netting, as reversões de posição são executadas fechando a exposição oposta antes de abrir um novo trade, exatamente como o EA original fazia com ordens de mercado.

## Diferenças do EA original

- Verificações de objetos de retração Fibonacci não estão presentes porque StockSharp não pode acessar desenhos de gráfico MT4. A estratégia sempre se comporta como o ramo de teste do código MQL.
- Os parâmetros de gestão de dinheiro (`Lots`, `LotExponent` e `Max_Trades`) foram substituídos por uma única propriedade `TradeVolume` porque as estratégias StockSharp operam em posições líquidas.
- Todas as rotinas de logging e alerta (`Alert`, `SendMail`, `SendNotification`) foram removidas intencionalmente.

Com esses ajustes, o porte StockSharp permanece fiel à lógica de trading do `FIBO1.mq4` enquanto fornece uma implementação limpa e parametrizada.
