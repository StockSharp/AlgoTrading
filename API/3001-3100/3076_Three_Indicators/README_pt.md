# Estratégia Três Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão do StockSharp do expert original **"Three indicators"** do MQL5. Ela avalia três osciladores clássicos — MACD, Oscilador Estocástico e RSI — em cada candle finalizado do período selecionado. Somente quando todos os filtros se alinham a estratégia entra em uma posição, garantindo que cada operação siga uma confirmação multi-indicador consistente.

## Lógica de trading
1. **Filtro de direção do candle** – compara o preço de abertura do candle finalizado atual com o da abertura anterior. Uma abertura mais alta favorece operações compradas, uma abertura mais baixa favorece vendidas.
2. **Filtro de inclinação MACD** – observa a inclinação da linha principal MACD (diferença entre o valor MACD principal atual e o anterior). Um MACD em queda favorece posições compradas, um MACD em alta favorece vendidas, exatamente como no expert de origem.
3. **Filtro de viés estocástico** – verifica se o valor %D está abaixo ou acima do ponto médio 50. Valores abaixo de 50 apoiam comprados, valores acima de 50 apoiam vendidos.
4. **Filtro de viés RSI** – usa o valor RSI relativo a 50. Valores abaixo de 50 autorizam comprados, valores acima de 50 autorizam vendidos.

Somente se **todos os quatro filtros** apoiarem a mesma direção a estratégia abrirá uma nova operação. Se um sinal oposto aparecer enquanto uma posição está aberta, a estratégia reverte imediatamente enviando uma única ordem de mercado que fecha a exposição existente e abre a nova direção, espelhando o comportamento da lógica MQL original.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período dos candles fornecidos à estratégia. Padrão: 1 minuto. |
| `TradeVolume` | Volume usado ao abrir uma posição ou reverter para o lado oposto. |
| `MacdFastPeriod` | Comprimento da EMA rápida no cálculo MACD. |
| `MacdSlowPeriod` | Comprimento da EMA lenta no cálculo MACD. |
| `MacdSignalPeriod` | Comprimento da EMA para a linha de sinal MACD. |
| `MacdPriceType` | Preço aplicado ao indicador MACD (Close, Open, High, Low, Median, Typical, Weighted). |
| `StochasticKPeriod` | Período de retrocesso para a linha %K. |
| `StochasticDPeriod` | Período de suavização para a linha %D. |
| `StochasticSlowing` | Suavização adicional aplicada a %K antes do cálculo de %D. |
| `RsiPeriod` | Período de média usado pelo filtro RSI. |
| `RsiPriceType` | Preço aplicado ao alimentar o indicador RSI. |

## Indicadores
- **MACD (Convergência/Divergência de Médias Móveis)** – configurado com os comprimentos rápido, lento e de sinal especificados pelo usuário.
- **Oscilador Estocástico** – usa a implementação do StockSharp com comprimentos %K/%D e suavização configuráveis.
- **Índice de Força Relativa (RSI)** – fornece a confirmação de impulso final.

## Notas de comportamento
- A estratégia processa apenas **candles finalizados**, melhorando a estabilidade em comparação com o gatilho baseado em ticks do expert original.
- A pausa de 30 segundos presente na versão MQL é removida; as reversões são emitidas imediatamente com a ordem de mercado combinada.
- A suavização estocástica usa a implementação de média móvel padrão do StockSharp, que corresponde à suavização padrão baseada em SMA do script original.
- A seleção de fonte de preço para MACD e RSI é fornecida através do enum `IndicatorAppliedPrice`, correspondendo às opções disponíveis no MetaTrader (Close, Open, High, Low, Median, Typical, Weighted).

## Gerenciamento de risco
Nenhuma ordem de stop-loss ou take-profit é colocada automaticamente. O gerenciamento de posição é conduzido exclusivamente pela lógica de reversão multi-indicador. Adicionar controles de risco externos se necessário.

## Dicas de uso
1. Selecionar o instrumento e período desejados através de `CandleType`.
2. Ajustar os parâmetros do indicador para se adequar à volatilidade do mercado e frequência de sinais.
3. Monitorar os objetos de gráfico adicionados pela estratégia (candles mais os três indicadores) para validar o alinhamento de sinais.
4. Combinar com gerenciamento de dinheiro externo se stops fixos ou alvos de lucro forem necessários.
