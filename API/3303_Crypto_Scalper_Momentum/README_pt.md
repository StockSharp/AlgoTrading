# Estratégia Crypto Scalper Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Crypto Scalper Momentum** replica o expert advisor original "Crypto Scalper" do MetaTrader combinando Money Flow Index, Momentum e filtros MACD multi-timeframe. Ela opera em um timeframe intradiário primário, confirma momentum de curto prazo em um timeframe superior e respeita um filtro de macrotendência derivado de um MACD lento. Diversos recursos de gestão de risco da implementação MQL foram preservados, incluindo metas de cesta baseadas em moeda, trailing monetário, stops de break-even e proteção contra drawdown do patrimônio.

## Lógica de negociação

1. **Indicadores primários**
   - Money Flow Index (MFI) no timeframe principal com padrão de 14 períodos.
   - MACD no timeframe principal (configuração EMA 12/26/9).
2. **Momentum de timeframe superior**
   - Indicador Momentum calculado em uma série de candles separada. A distância absoluta em relação à linha-base do MetaTrader (100) deve exceder um limiar configurável.
3. **Filtro de macrotendência**
   - Um MACD lento avaliado em um timeframe macro (diário por padrão) impede operar contra a tendência superior e força liquidação quando ela reverte.
4. **Regras de entrada**
   - **Compras**: pelo menos um dos três últimos valores de MFI está abaixo do limiar de sobrevenda, o desvio de momentum excede o limiar, a linha MACD principal está acima da linha de sinal e o MACD macro é altista.
   - **Vendas**: condições espelhadas usando limiares de sobrecompra e confirmações MACD baixistas.
5. **Regras de saída**
   - Stop-loss e take-profit fixos expressos em pips.
   - Trailing stop opcional por extremos de candles ou trailing clássico baseado em distância.
   - Movimento para break-even após uma excursão favorável configurável.
   - Reversão do MACD macro fecha a exposição existente.
   - Metas em moeda, metas percentuais e trailing de lucro em dinheiro replicam os recursos MQL.
   - Um monitor de drawdown do patrimônio fecha todas as operações quando a conta recua uma porcentagem definida a partir do pico.

## Gestão de risco

- **Stops/metas**: distâncias configuráveis em pips com habilitação opcional.
- **Trailing**: baseado em candles (menor mínima/maior máxima de candles recentes) ou trailing clássico em pips.
- **Break-even**: move stops para travar lucros quando a distância de gatilho é alcançada.
- **Gestão monetária**: take-profit de cesta em moeda, percentual do patrimônio inicial e trailing de lucro em dinheiro.
- **Stop de patrimônio**: monitora o maior patrimônio observado e fecha operações quando o drawdown excede a porcentagem permitida.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `CandleType` | Série principal de candles usada para entradas. |
| `MomentumCandleType` | Candles de timeframe superior que alimentam o indicador Momentum. |
| `MacroCandleType` | Candles de timeframe lento usados para o filtro MACD macro. |
| `MfiPeriod` | Comprimento do Money Flow Index. |
| `MfiOversold` / `MfiOverbought` | Limiares do oscilador (padrão 30 / 70). |
| `MomentumPeriod` | Comprimento do Momentum no timeframe superior. |
| `MomentumThreshold` | Desvio mínimo da linha 100 exigido pelo filtro de momentum. |
| `MomentumReference` | Valor de referência (o padrão do MetaTrader é 100). |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Parâmetros MACD no timeframe de negociação. |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | Parâmetros MACD no timeframe macro. |
| `TradeVolume` | Volume de cada ordem a mercado (lotes). |
| `MaxTrades` | Máximo de operações simultâneas por direção (0 = ilimitado). |
| `UseStopLoss` / `StopLossPips` | Habilita e configura o stop protetor. |
| `UseTakeProfit` / `TakeProfitPips` | Habilita e configura a meta protetora. |
| `UseTrailingStop` | Chave principal da lógica de trailing. |
| `UseCandleTrail` | Alterna entre trailing por extremo de candle e trailing clássico. |
| `TrailTriggerPips` / `TrailAmountPips` | Distância de gatilho e distância mantida pelo trailing stop clássico. |
| `CandleTrailLength` / `CandleTrailBufferPips` | Número de candles e buffer extra para trailing baseado em candles. |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Distância de ativação do break-even e lucro travado. |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | Take-profit de cesta na moeda da conta. |
| `UsePercentTakeProfit` / `PercentTakeProfit` | Take-profit de cesta em percentual do patrimônio inicial. |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | Trailing do lucro flutuante em moeda. |
| `UseEquityStop` / `EquityRiskPercent` | Guarda de drawdown do patrimônio relativa ao pico observado. |
| `ForceExit` | Zera imediatamente as posições no próximo fechamento de candle. |

## Notas

- Distâncias em pips são convertidas com o `PriceStep` do instrumento. Um fallback de `0.0001` é usado se a corretora não fornecer passo de preço, igual ao tratamento de pontos no MetaTrader.
- A assinatura MACD macro pode apontar para candles mensais para imitar o EA original. Candles diários são o padrão porque barras mensais podem não estar disponíveis em todos os feeds de dados.
- Todos os comentários dentro do código são escritos em inglês para cumprir as regras do repositório.
