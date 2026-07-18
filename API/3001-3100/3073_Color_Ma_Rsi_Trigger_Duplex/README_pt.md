# Estratégia Color Ma RSI Trigger Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **Exp_ColorMaRsi-Trigger_Duplex.mq5** para a API de alto nível do StockSharp.
Ela opera dois detectores MaRsi-Trigger independentes: o **bloco comprado** decide quando posições compradas devem ser abertas ou fechadas, enquanto o **bloco vendido** realiza a mesma tarefa para posições vendidas. Cada detector avalia se um indicador personalizado reporta pressão de mercado altista (`+1`), neutra (`0`) ou baixista (`-1`). A lógica original do MetaTrader é preservada, incluindo a confirmação atrasada que aguarda duas barras completas antes de reagir e as configurações de gerenciamento de dinheiro separadas por direção.

## Ideia de trading

1. Calcular duas médias móveis exponenciais (rápida e lenta) e dois osciladores RSI (rápido e lento) em uma série de candles selecionável para cada bloco.
2. Em cada candle finalizado o indicador retorna `+1` quando ambos os estudos rápidos dominam suas contrapartes lentas, `-1` quando ambos são mais fracos e `0` caso contrário. O valor bruto é limitado ao intervalo `[-1, 1]` como no indicador MT5.
3. A estratégia armazena um histórico contínuo de valores do indicador. Para um deslocamento `SignalBar` configurado, ela compara o valor da barra `SignalBar + 1` períodos atrás (chamado `older`) com o valor da barra `SignalBar` períodos atrás (chamado `recent`).
4. Lógica comprada:
   - Se `older < 0` o bloco comprado fecha qualquer posição comprada ativa (desde que as saídas compradas estejam habilitadas).
   - Se `older > 0` **e** `recent <= 0` o bloco comprado prepara uma nova entrada comprada (desde que as entradas compradas estejam habilitadas).
5. A lógica vendida espelha o bloco comprado:
   - Se `older > 0` o bloco vendido sai de posições vendidas existentes (quando as saídas vendidas estão habilitadas).
   - Se `older < 0` **e** `recent >= 0` o bloco abre uma nova posição vendida (quando as entradas vendidas estão habilitadas).
6. Níveis opcionais de stop-loss e take-profit, expressos em passos de preço do instrumento, encerram posições quando o preço cruza os níveis configurados.

Os dois blocos podem assinar diferentes períodos de candles e fontes de preço, permitindo ao usuário replicar o comportamento dual de períodos original ou experimentar com combinações alternativas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `LongCandleType`, `ShortCandleType` | Séries de dados de candles usadas pelos blocos comprado e vendido. Padrão velas de 4 horas. |
| `LongVolume`, `ShortVolume` | Volume de mercado negociado quando o bloco correspondente abre uma nova posição. |
| `LongAllowOpen`, `ShortAllowOpen` | Habilitar ou desabilitar a abertura de novas posições para cada bloco. |
| `LongAllowClose`, `ShortAllowClose` | Habilitar ou desabilitar sinais de fechamento para cada bloco. |
| `LongStopLossPoints`, `ShortStopLossPoints` | Distância de stop-loss medida em passos de preço. Definir como `0` para desabilitar. |
| `LongTakeProfitPoints`, `ShortTakeProfitPoints` | Distância de take-profit medida em passos de preço. Definir como `0` para desabilitar. |
| `LongSignalBar`, `ShortSignalBar` | Número de barras completadas entre o candle atual e o usado para a lógica de decisão. |
| `LongRsiPeriod`, `LongRsiLongPeriod`, `ShortRsiPeriod`, `ShortRsiLongPeriod` | Comprimentos dos osciladores RSI rápido e lento. |
| `LongMaPeriod`, `LongMaLongPeriod`, `ShortMaPeriod`, `ShortMaLongPeriod` | Comprimentos das médias móveis rápida e lenta. |
| `LongRsiPrice`, `ShortRsiPrice` | Fonte de preço alimentada ao RSI rápido (fechamento, abertura, máximo, mínimo, mediana, típico ou ponderado). |
| `LongRsiLongPrice`, `ShortRsiLongPrice` | Fonte de preço alimentada ao RSI lento. |
| `LongMaPrice`, `ShortMaPrice` | Fonte de preço alimentada à média móvel rápida. |
| `LongMaLongPrice`, `ShortMaLongPrice` | Fonte de preço alimentada à média móvel lenta. |
| `LongMaType`, `ShortMaType` | Método de média móvel para a linha rápida (simples, exponencial, suavizada ou ponderada). |
| `LongMaLongType`, `ShortMaLongType` | Método de média móvel para a linha lenta. |

## Regras de trading

1. Aguardar até que a série de candles selecionada produza barras finalizadas e todos os indicadores estejam completamente aquecidos.
2. Para cada bloco calcular o valor MaRsi-Trigger e atualizar o buffer de histórico.
3. Quando o histórico contém pelo menos `SignalBar + 2` entradas, avaliar as condições compradas e vendidas descritas na seção de ideia de trading.
4. Antes de abrir uma posição a estratégia neutralizará qualquer exposição oposta (se a flag de fechamento correspondente estiver habilitada). Por exemplo, uma nova entrada comprada comprará volume suficiente para fechar uma posição vendida e só então adicionará o volume comprado.
5. Após uma posição ser aberta, os níveis opcionais de stop-loss e take-profit são monitorados em cada candle finalizado.
6. As ordens de abertura e fechamento são enviadas como ordens de mercado pelos helpers de alto nível `BuyMarket` e `SellMarket`.

## Gerenciamento de risco

* Stops e alvos são medidos usando `Security.PriceStep`. Quando o instrumento não expõe um passo de preço, um valor padrão de `1` é assumido, correspondendo ao comportamento de muitas estratégias existentes neste repositório.
* Os blocos comprado e vendido mantêm configurações independentes de stop e take.
* A estratégia não coloca ordens protetoras adicionais (como stops de seguimento); o comportamento espelha o expert MT5, que fecha operações apenas quando o indicador dispara ou quando o stop/objetivo duro é atingido.

## Notas

* O port do StockSharp emite ordens de mercado imediatamente após o candle de avaliação ser finalizado. No MetaTrader o expert agendava suas ordens para o tempo de abertura da próxima barra via deslocamentos de timestamp; ambos os comportamentos se alinham efetivamente porque o StockSharp processa o sinal assim que o candle fecha.
* O EA original expunha vários modos de gerenciamento de dinheiro (`LOT`, `BALANCE`, etc.). As estratégias do StockSharp trabalham com valores de volume diretos, portanto o port mantém o volume como parâmetro direto (`LongVolume`/`ShortVolume`).
* O slippage e a lógica específica de número mágico da biblioteca auxiliar MT5 não são necessários no StockSharp e foram omitidos.
* Os cálculos do indicador aproveitam as implementações integradas de médias móveis e RSI do StockSharp; a saída é limitada a `[-1, 1]` para corresponder ao indicador original `ColorMaRsi-Trigger`.
