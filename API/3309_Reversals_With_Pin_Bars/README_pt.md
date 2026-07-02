# Estratégia Reversals With Pin Bars
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port C# do expert advisor **"Reversals With Pin Bars"** do MetaTrader. O EA original procura candles de rejeição com sombras longas (pin bars) e os confirma com um filtro de momentum, uma checagem de tendência por média móvel linearmente ponderada (LWMA) em timeframe superior e um filtro direcional MACD. O port mantém essa estrutura multi-timeframe, usa exclusivamente indicadores StockSharp e expõe os controles de risco mais importantes como parâmetros.

A implementação foca na API de alto nível do StockSharp: candles do timeframe primário conduzem entradas, enquanto assinaturas adicionais alimentam indicadores de timeframes superiores. A gestão de risco é expressa em pips e oferece suporte opcional a trailing-stop e automação de break-even.

## Lógica de entrada
- **Detecção de pin bar**: o candle anterior concluído deve ter uma sombra que represente pelo menos 50% de todo o seu range.
  - Configuração comprada: a sombra superior é dominante (correspondendo à verificação original de "hanging man").
  - Configuração vendida: a sombra inferior é dominante.
- **Filtro de tendência**: a LWMA rápida (comprimento = `FastMaPeriod`) deve estar acima/abaixo da LWMA lenta (`SlowMaPeriod`) no timeframe superior.
- **Filtro de momentum**: a distância absoluta do valor de momentum em relação a 100 em qualquer uma das três últimas barras do timeframe superior deve exceder `MomentumThreshold`.
- **Filtro MACD**: a linha principal MACD deve estar acima/abaixo da linha de sinal no timeframe MACD.
- **Limites de posição**: a exposição líquida não pode exceder `MaxTrades * Volume`. Novas operações usam a configuração alinhada `Volume`.

## Gestão de risco
- **Stop-loss / Take-profit**: distâncias fixas em pips (`StopLossPips`, `TakeProfitPips`) a partir do fechamento de entrada.
- **Break-even**: quando habilitado, o stop se move para `entry +/- BreakEvenOffsetPips` quando o preço avança `BreakEvenTriggerPips`.
- **Trailing stop**: quando habilitado, o trailing mantém uma distância de `TrailingStopPips` do último fechamento.
- **Zeragem automática**: atingir o stop ou alvo calculado encerra a posição inteira com uma ordem a mercado.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `TradeVolume` | Volume usado para cada nova entrada, alinhado ao passo do instrumento. |
| `MaxTrades` | Número máximo de entradas na mesma direção (limite de volume agregado). |
| `StopLossPips` | Distância do stop-loss em pips. |
| `TakeProfitPips` | Distância do take-profit em pips. |
| `EnableTrailing` / `TrailingStopPips` | Habilita e configura a distância do trailing-stop. |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Ativação do break-even e configurações de buffer. |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos das LWMAs do timeframe superior. |
| `MomentumPeriod` / `MomentumThreshold` | Comprimento do momentum e distância absoluta mínima de 100. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuração MACD para o filtro de longo prazo. |
| `CandleType` | Série principal de candles para detecção de pin bars. |
| `HigherCandleType` | Série de candles usada para LWMAs e momentum. |
| `MacdCandleType` | Série de candles usada para MACD. |

## Diferenças em relação à versão MetaTrader
- Opções de take-profit monetário, trailing e stop de patrimônio foram omitidas; o risco é expresso por pips.
- Confirmações por linhas fractais que exigiam objetos gráficos foram substituídas por condições baseadas em indicadores.
- Todas as notificações (alertas, e-mails, mensagens push) foram removidas; a versão StockSharp se concentra na lógica de negociação.

## Notas de uso
1. Atribua a estratégia a uma carteira e ativo, depois ajuste os três tipos de candle ao seu setup multi-timeframe desejado.
2. Garanta que o passo de preço do instrumento reflita a definição de pip (fallback padrão de 0.0001).
3. Inicie a estratégia; stops, metas, trailing e gestão de break-even são realizados automaticamente no fechamento do candle.
4. Monitore os resultados; ajuste comprimentos de momentum e LWMA ao perfil de volatilidade do instrumento.
