# Simple EA MA plus MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia porta o consultor especializado MetaTrader 5 **Simple EA MA plus MACD** para a API de alto nível do StockSharp. Busca um rompimento de uma "barra de sinal" que satisfaz duas condições: uma média móvel deslocada está abaixo/acima das máximas da barra, e o histograma MACD acabou de cruzar a linha zero. Quando o próximo candle fecha além do extremo da barra de sinal, a estratégia entra na direção do rompimento.

A implementação mantém o comportamento original do EA:

1. **Detecção de sinal** – em cada candle finalizado a estratégia inspeciona a barra anterior. Uma média móvel configurável (padrão LWMA) calculada no preço aplicado escolhido deve ser menor que as máximas dos candles anterior e atual para comprados (maior para vendidos). Simultaneamente a linha principal MACD deve ter cruzado zero entre as duas barras precedentes.
2. **Confirmação de sinal** – uma vez armazenada uma barra de sinal, a estratégia aguarda o próximo candle completado. Um fechamento acima da máxima armazenada aciona um rompimento comprado; um fechamento abaixo da mínima armazenada aciona um rompimento vendido. Se o preço invalida o sinal ao fechar de volta dentro da barra de sinal, a configuração é cancelada.
3. **Gestão de posição** – novos trades herdam distâncias de stop-loss, take-profit e trailing-stop expressas em pips. Os níveis de proteção são convertidos para preços absolutos usando o `PriceStep` do instrumento. Instrumentos com três ou cinco decimais recebem o ajuste clássico de forex (step × 10) para imitar as definições de pip do MetaTrader.

## Gestão de risco
- **Stop-loss / take-profit** – distâncias opcionais definidas em pips são avaliadas em cada fechamento de candle. Quando o mercado imprime além do nível correspondente, a estratégia sai com uma ordem de mercado.
- **Trailing stop** – quando o lucro supera `TrailingStopPips + TrailingStepPips`, uma referência de trailing é movida atrás do melhor preço alcançado. Se o preço recuar ao nível de trailing, a posição é fechada. Um passo de trailing zero reativa o stop em cada novo extremo.
- **Zerar na reversão** – se um rompimento oposto aparecer enquanto uma posição oposta está aberta, a estratégia envia uma única ordem de mercado suficientemente grande para fechar a exposição existente e abrir o novo trade em um único movimento.

## Notas de implementação
- A média móvel suporta os mesmos métodos de suavização e opções de preço aplicado que o MetaTrader (Simple, Exponential, Smoothed, LinearWeighted e preços Close/Open/High/Low/Median/Typical/Weighted).
- `MaShift` reproduz o deslocamento horizontal do indicador MetaTrader lendo valores de barras anteriores antes de avaliar as regras de rompimento.
- MACD usa o indicador integrado `MovingAverageConvergenceDivergence`. Apenas o histograma (diferença entre EMAs rápida e lenta) é necessário; o período da linha de sinal é retido para manter paridade com as configurações do EA.
- As assinaturas de candles e o processamento de indicadores dependem exclusivamente da API de alto nível do StockSharp. Nenhum tratamento manual de ticks ou buffers de indicadores são usados.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Volume` | `1` | Tamanho da ordem para cada entrada de rompimento. |
| `TakeProfitPips` | `50` | Distância do alvo de lucro em pips (convertida para preço absoluto usando o passo de preço do instrumento). Definir como 0 para desativar. |
| `StopLossPips` | `50` | Distância do stop de proteção em pips. Definir como 0 para desativar. |
| `TrailingStopPips` | `5` | Distância do trailing stop em pips que é fixada quando o preço avança suficientemente. |
| `TrailingStepPips` | `5` | Progresso adicional mínimo (em pips) antes de o trailing stop avançar novamente. |
| `MaPeriod` | `100` | Comprimento da média móvil usada para validar a barra de sinal. |
| `MaShift` | `0` | Deslocamento horizontal aplicado à média móvil, emulando o parâmetro `ma_shift` do MetaTrader. |
| `MaMethod` | `LinearWeighted` | Método de suavização da média móvil (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaAppliedPrice` | `Weighted` | Fonte de preço alimentada na média móvil (Close, Open, High, Low, Median, Typical, Weighted). |
| `MacdFastPeriod` | `12` | Período EMA rápida usado no cálculo MACD. |
| `MacdSlowPeriod` | `26` | Período EMA lenta usado no cálculo MACD. |
| `MacdSignalPeriod` | `9` | Período de suavização da linha de sinal retido para paridade com o EA original. |
| `MacdAppliedPrice` | `Weighted` | Preço aplicado ao alimentar valores no MACD. |
| `CandleType` | `1 hour` time frame | Série de candles principal analisada para sinais e gestão de trades. |

## Dicas de uso
- Ajustar as proteções baseadas em pip para corresponder ao tamanho de tick do instrumento selecionado; valores `PriceStep` incorretos no lado do conector distorcerão as conversões de pip.
- Para mercados altamente voláteis, considerar aumentar `TrailingStepPips` para reduzir saídas prematuras, ou diminuí-lo para ajustar o comportamento de trailing.
- Como os trades são executados em candles fechados, o rompimento deve persistir até que a barra seja concluída; habilitar períodos menores aumenta a frequência de negociação mas pode introduzir mais ruído.
