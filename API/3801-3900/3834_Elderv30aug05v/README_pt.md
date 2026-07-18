# Estratégia Elderv30aug05v
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Elderv30aug05v é uma porta direta do MetaTrader 4 consultor especialista com o mesmo nome. Ele combina sinais de dois filtros MACD calculados em velas horárias e dois osciladores estocásticos calculados em velas de 15 minutos. A execução da negociação e o gerenciamento de saída acontecem em velas de um minuto para replicar a lógica tick-by-tick do script MQL original. A estratégia abre no máximo uma posição por vez e depende de trailing stops dinâmicos em vez de ordens fixas de take-profit.

## Indicadores e Dados
- **Primário MACD** (`13/30/9`, velas horárias). Um sinal longo exige que o histograma se incline para cima enquanto o valor anterior permanece abaixo de zero.
- **Secundário MACD** (`14/56/9`, velas horárias). Um sinal curto exige que o histograma se incline para baixo enquanto o valor anterior permanece acima de zero.
- **Oscilador estocástico rápido** (`%K=2`, `%D=3`, suavização=3, velas de 15 minutos). Entradas longas exigem que a linha %K fique abaixo do teto configurado (padrão 36) e subindo em relação à barra anterior.
- **Oscilador estocástico lento** (`%K=1`, `%D=3`, suavização=3, velas de 15 minutos). As entradas curtas exigem que a linha %K esteja acima do piso configurado (padrão 66) e diminuindo em relação à barra anterior.
- **Velas de um minuto** fornecem os dados de confirmação para verificações de breakout e gerenciam trailing stops.

Todos os indicadores processam apenas velas concluídas por meio de `SubscribeCandles().Bind()/BindEx()` para seguir as diretrizes de alto nível StockSharp API.

## Regras de entrada
### Configuração longa
1. O valor primário MACD está acima da leitura anterior e a leitura anterior é negativa.
2. O estocástico rápido %K está abaixo de `LongStochasticThreshold` (padrão 36) e acima de seu valor anterior.
3. O fechamento da vela atual de um minuto é maior que a máxima da vela anterior de um minuto.

### Configuração curta
1. O valor secundário MACD está abaixo da leitura anterior e a leitura anterior é positiva.
2. O estocástico lento %K está acima de `ShortStochasticThreshold` (padrão 66) e abaixo de seu valor anterior.
3. O fechamento da vela atual de um minuto é inferior à mínima da vela anterior de um minuto.

Apenas uma posição pode ser aberta. Se um novo sinal aparecer enquanto uma posição estiver ativa, ele será ignorado até que a posição seja fechada por stop-loss ou lógica de trailing.

## Regras de saída
- **Stop-loss inicial**: Na entrada, a estratégia armazena o preço de entrada mais/menos `LongStopLoss` ou `ShortStopLoss` multiplicado pelo instrumento `PriceStep`. Se `PriceStep` não for fornecido, um substituto de `0.0001` será usado.
- **Trailing stop**: quando o preço se move a favor da negociação em pelo menos `LongTrailingStop` ou `ShortTrailingStop` pontos (novamente multiplicado por `PriceStep`), o preço stop armazenado é deslocado para trás do mercado. Para negociações longas, o stop segue o fechamento menos a distância final e só se move para cima. Para negociações curtas, o stop segue o fechamento mais a distância e só se move para baixo.
- Quando o intervalo da vela atinge o preço stop armazenado, a posição é fechada no mercado.

Nenhum nível fixo de lucro é usado, refletindo o comportamento original MetaTrader.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `0.1` | Volume de negociação enviado para `BuyMarket`/`SellMarket`. |
| `LongStopLoss` | `17` | Longa distância de stop-loss em pontos. |
| `ShortStopLoss` | `46` | Distância curta de stop-loss em pontos. |
| `LongTrailingStop` | `18` | Distância final para posições longas. |
| `ShortTrailingStop` | `22` | Distância final para posições curtas. |
| `LongStochasticThreshold` | `36` | Valor máximo estocástico rápido de %K para entradas longas. |
| `ShortStochasticThreshold` | `66` | Valor mínimo estocástico lento de %K para entradas curtas. |
| `BaseCandleType` | `TimeFrame(1m)` | Série de velas usada para lógica de execução. |
| `StochasticCandleType` | `TimeFrame(15m)` | Série de velas para ambos os osciladores estocásticos. |
| `MacdCandleType` | `TimeFrame(1h)` | Série de velas para ambos os filtros MACD. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | `13 / 30 / 9` | Períodos para o MACD primário. |
| `AltMacdFastPeriod` / `AltMacdSlowPeriod` / `AltMacdSignalPeriod` | `14 / 56 / 9` | Períodos para o secundário MACD. |
| `StochasticFastKPeriod` / `StochasticFastDPeriod` / `StochasticFastSmooth` | `2 / 3 / 3` | Parâmetros para o estocástico rápido. |
| `StochasticSlowKPeriod` / `StochasticSlowDPeriod` / `StochasticSlowSmooth` | `1 / 3 / 3` | Parâmetros para o estocástico lento. |

## Notas
- A estratégia funciona com qualquer instrumento que forneça velas de nível minuto e um `PriceStep` válido.
- Os trailing stops são mantidos internamente; nenhuma ordem de proteção é registrada no lado da bolsa.
- A lógica processa apenas velas concluídas para evitar repintura e corresponde à implementação MQL que dependia de barras concluídas.

## Roteiro Original
- **Fonte**: `MQL/7674/Elderv30aug05v.mq4`
- **Plataforma**: MetaTrader 4 consultores especialistas.
