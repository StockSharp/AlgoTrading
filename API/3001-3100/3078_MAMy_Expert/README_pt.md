# Estratégia MAMy Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Port do consultor do MetaTrader 5 "MAMy Expert" de Victor Chebotariov para a API de estratégia de alto nível do StockSharp.
- Reproduz o indicador personalizado original que compara três médias móveis de diferentes fontes de preço (abertura, fechamento, preço ponderado).
- Funciona estritamente com candles concluídos e gerencia no máximo uma posição líquida por vez, espelhando o comportamento do expert MQL.

## Base do indicador
- A estratégia constrói três médias móveis usando o mesmo comprimento e algoritmo de suavização:
  - `MA(close)` – calculada sobre os preços de fechamento dos candles.
  - `MA(open)` – calculada sobre os preços de abertura dos candles.
  - `MA(weighted)` – calculada sobre o preço ponderado `(High + Low + 2 × Close) / 4`.
- O parâmetro `MaType` seleciona o algoritmo de média (Simples, Exponencial, Suavizado ou LWMA Ponderado) para as três séries, correspondendo às opções `MODE_*` do MetaTrader.
- Um "buffer de fechamento" é calculado como a diferença `MA(close) − MA(weighted)`.
- Um "buffer de abertura" potencial é produzido apenas quando as médias móveis se alinham em uma configuração de tendência:
  - **Configuração de baixa**: tanto `MA(close)` quanto `MA(weighted)` caem, a MA de fechamento permanece abaixo da MA ponderada, ambas permanecem abaixo da MA de abertura, e o buffer de fechamento diminui.
  - **Configuração de alta**: tanto `MA(close)` quanto `MA(weighted)` sobem, a MA de fechamento permanece acima da MA ponderada, ambas permanecem acima da MA de abertura, e o buffer de fechamento aumenta.
  - Quando qualquer configuração é verdadeira, o buffer de abertura torna-se `(MA(weighted) − MA(open)) + (MA(close) − MA(weighted))`; caso contrário é redefinido como zero.
- Se um buffer de abertura positivo novo acompanha um cruzamento negativo do buffer de fechamento, o buffer de fechamento é forçado a zero, assim como no código do indicador original.

## Lógica de sinais
- **Condições de entrada**
  - **Comprar** quando o buffer de abertura cruza para cima por zero (`anterior ≤ 0`, `atual > 0`).
  - **Vender** quando o buffer de abertura cruza para baixo por zero (`anterior ≥ 0`, `atual < 0`).
  - As entradas são consideradas apenas quando não há posição existente.
- **Condições de saída**
  - **Fechar comprado** quando o buffer de fechamento cruza abaixo de zero (`anterior ≥ 0`, `atual < 0`).
  - **Fechar vendido** quando o buffer de fechamento cruza acima de zero (`anterior ≤ 0`, `atual > 0`).
  - As saídas são avaliadas antes das novas entradas, então a estratégia nunca mantém exposição comprada e vendida simultânea.
- As ordens são emitidas a mercado usando o `TradeVolume` configurado. A automatização protetora via `StartProtection()` espelha a chamada de segurança nos exemplos do StockSharp.

## Gráficos e fluxo de dados
- Assina o período definido por `CandleType` e processa apenas candles concluídos.
- Desenha candles de preço junto com as três médias móvies e anota ordens executadas, fornecendo as mesmas dicas visuais que o indicador original entregava no MetaTrader.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Período principal que fornece candles para o indicador e sinais. |
| `MaPeriod` | `int` | `3` | Comprimento aplicado às três médias móvies. |
| `MaType` | `MaCalculationType` | `Weighted` | Algoritmo de média (Simples, Exponencial, Suavizado, Ponderado). |
| `TradeVolume` | `decimal` | `1` | Volume usado para cada entrada de ordem de mercado. |

## Notas de implementação
- Usa o fluxo de trabalho de alto nível `SubscribeCandles().Bind(...)` e os indicadores de média móvel integrados do StockSharp; nenhum buffer personalizado é armazenado além dos últimos valores necessários para detecção de sinais.
- Os sinais são avaliados apenas depois que todos os indicadores estão completamente formados e a estratégia está pronta para trading ao vivo (`IsFormedAndOnlineAndAllowTrading()`).
- A estratégia ignora intencionalmente entradas concorrentes enquanto uma posição está aberta, correspondendo fielmente à lógica do consultor especialista de origem.
