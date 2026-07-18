# Estratégia Exp Índice de Intensidade de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp do especialista MetaTrader **Exp_Trend_Intensity_Index**. Ela opera em velas finalizadas em um período configurável e usa o Índice de Intensidade de Tendência (TII) para detectar quando o momentum sai de zonas altistas ou baixistas extremas. Quando o indicador faz a transição para fora de uma zona superior, o algoritmo fecha posições vendidas e pode iniciar uma nova posição comprada. Quando o indicador sai de uma zona inferior, o algoritmo fecha posições compradas e pode iniciar uma nova posição vendida.

## Como o indicador é construído

1. Selecionar a fonte de preço (close, open, variantes ponderadas, preços de acompanhamento de tendência, etc.).
2. Suavizar esse fluxo de preços com uma primeira média móvel (`PriceMaMethod`, `PriceMaLength`).
3. Dividir a diferença entre o preço e o valor suavizado em fluxos positivos e negativos.
4. Suavizar os fluxos positivos e negativos independentemente com uma segunda média móvel (`SmoothingMethod`, `SmoothingLength`).
5. Calcular o Índice de Intensidade de Tendência: `TII = 100 * Positive / (Positive + Negative)`.
6. Comparar o resultado com os limites `HighLevel` e `LowLevel` para atribuir um estado de cor: zona alta (`0`), neutro (`1`) ou zona baixa (`2`).

A implementação usa médias móveis do StockSharp (simples, exponencial, suavizada, ponderada). Tipos de suavização avançados da biblioteca MQL original não estão disponíveis neste port.

## Lógica de trading

* Os sinais são processados apenas quando uma vela está completamente fechada (`CandleStates.Finished`).
* O parâmetro `SignalBar` define qual barra completada é analisada (padrão: uma barra atrás). A estratégia também inspeciona a barra imediatamente anterior, correspondendo à busca de buffer duplo no código MQL.
* Quando a barra mais antiga pertence à zona alta (`color == 0`):
  * Fechar qualquer posição vendida se `EnableSellExits` for verdadeiro.
  * Se a barra mais recente saiu da zona alta e `EnableBuyEntries` for verdadeiro, abrir ou reverter para uma posição comprada.
* Quando a barra mais antiga pertence à zona baixa (`color == 2`):
  * Fechar qualquer posição comprada se `EnableBuyExits` for verdadeiro.
  * Se a barra mais recente saiu da zona baixa e `EnableSellEntries` for verdadeiro, abrir ou reverter para uma posição vendida.
* As ordens são enviadas com `BuyMarket` e `SellMarket`. As reversões de posição usam o volume de posição atual mais a propriedade `Volume` configurada.
* A proteção opcional de stop-loss e take-profit (unidades de preço) é configurada através de `StopLossPoints` e `TakeProfitPoints` e implementada com `StartProtection`.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período usado para cálculo do indicador e negociação. |
| `PriceMaMethod`, `PriceMaLength` | Tipo e período da média móvel aplicada ao fluxo de preço base. |
| `SmoothingMethod`, `SmoothingLength` | Tipo e período da média móvel aplicada aos fluxos positivos e negativos. |
| `AppliedPrice` | Fonte de preço para o indicador (close, open, median, variantes de acompanhamento de tendência, Demark, etc.). |
| `HighLevel`, `LowLevel` | Limites superiores e inferiores que definem zonas altistas e baixistas. |
| `SignalBar` | Número de barras completadas para olhar para trás para confirmação de sinal. |
| `EnableBuyEntries`, `EnableSellEntries` | Interruptores que permitem abrir posições compradas/vendidas. |
| `EnableBuyExits`, `EnableSellExits` | Interruptores que permitem saídas automáticas quando o indicador muda. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias protetoras opcionais expressas em unidades de preço para `StartProtection`. |

## Diferenças do especialista MQL original

* As opções de gestão monetária (`MM`, `MMMode`, `Deviation`) são substituídas pela propriedade de volume padrão do StockSharp e execução de ordens; o gerenciamento de slippage não é replicado.
* Apenas os tipos de média móvel disponíveis no StockSharp (simples, exponencial, suavizada, ponderada) são suportados.
* Os parâmetros de fase do indicador MQL são omitidos porque os indicadores do StockSharp não expõem controles equivalentes.
* As ordens são executadas imediatamente após a confirmação de um sinal na vela finalizada; não há agendamento explícito para a próxima abertura de barra.

Essas mudanças mantêm a ideia de negociação intacta enquanto seguem as diretrizes de estratégia de alto nível do StockSharp.
