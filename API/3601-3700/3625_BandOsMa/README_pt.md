# Estratégia BandOsMa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia BandOsMa** converte o consultor especialista MetaTrader 5 "BandOsMA" em uma estratégia StockSharp. Ele avalia o histograma MACD (OsMA) usando Bollinger bandas construídas diretamente nos valores do histograma. Os rompimentos acima ou abaixo das bandas criam sinais de entrada, enquanto uma média móvel adicional do histograma gerencia as saídas de sinal.

A estratégia opera em um único símbolo e prazo selecionado pelo usuário. Os valores dos indicadores são calculados em velas finalizadas usando assinaturas de velas de alto nível de StockSharp.

## Lógica de negociação
1. **Indicadores**
   - `MovingAverageConvergenceDivergenceSignal` fornece o histograma MACD (OsMA).
   - `BollingerBands` é aplicado à sequência OsMA para detectar desvios extremos.
   - Uma média móvel configurável suaviza o histograma e atua como um filtro de saída.
2. **Entrada**
   - Um **sinal longo** aparece quando o OsMA atual fecha abaixo da banda inferior enquanto a barra anterior permanece acima dela.
   - Um **sinal curto** aparece quando o OsMA atual fecha acima da banda superior enquanto a barra anterior permanece abaixo dela.
3. **Sair**
   - Os sinais são apagados quando o histograma cruza a média móvel na direção oposta.
   - Quando uma posição aberta não corresponde mais ao sinal ativo, a posição é fechada imediatamente.
   - Um stop loss baseado em pip é anexado a cada posição. A parada também atua como uma parada móvel com a mesma distância e um passo final igual a `StopLossPoints / 50` (espelhando a classe auxiliar MetaTrader).

## Gerenciamento de posição
- **Stop Loss e Trailing**: A distância de stop é expressa em MetaTrader pontos e convertida em unidades de preço usando o `PriceStep` do instrumento. A mesma distância é usada para o trailing stop, que avança quando o preço de fechamento melhora pelo menos um trailing step.
- **Uma posição por vez**: Apenas uma posição líquida é mantida. Os sinais opostos fecham a posição atual antes de considerar uma nova entrada.

## Parâmetros
| Grupo | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Geral | `CandleType` | Prazo para assinatura de velas e cálculo de indicadores. | `H1` |
| Risco | `LotSize` | Volume de negociação em lotes. | `0.01` |
| Risco | `StopLossPoints` | Distância de stop-loss expressa em MetaTrader pontos (também usada para trailing). | `1000` |
| Indicadores | `MacdFastPeriod` | Comprimento EMA rápido em MACD. | `12` |
| Indicadores | `MacdSlowPeriod` | Comprimento EMA lento em MACD. | `26` |
| Indicadores | `MacdSignalPeriod` | Comprimento do sinal EMA em MACD. | `9` |
| Indicadores | `PriceType` | Preço aplicado para entrada MACD (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Typical` |
| Indicadores | `BollingerPeriod` | Período de Bollinger bandas na sequência OsMA. | `26` |
| Indicadores | `BollingerShift` | Shift aplicado a Bollinger buffers (não negativo). | `0` |
| Indicadores | `BollingerDeviation` | Multiplicador de desvio padrão para Bollinger bandas. | `2` |
| Indicadores | `MovingAveragePeriod` | Comprimento da média móvel aplicada ao OsMA. | `10` |
| Indicadores | `MovingAverageShift` | Shift aplicado ao buffer de média móvel (não negativo). | `0` |
| Indicadores | `MovingAverageMethod` | Tipo de média móvel (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |

## Notas de implementação
- O processamento de velas usa `WhenCandlesFinished` para garantir que apenas as barras finais conduzam a lógica.
- Os valores dos indicadores são armazenados em buffers de histórico para emular mudanças de buffer no estilo MetaTrader. Mudanças negativas não são suportadas; use valores zero ou positivos como nos padrões originais do especialista.
- As paradas finais dependem do fechamento de velas, em vez de atualizações passo a passo. Ajuste a distância do pip se for necessário um rastreamento preciso no nível do tick.

## Uso
1. Selecione o símbolo e o período desejados em StockSharp.
2. Configure os parâmetros, especialmente `CandleType`, `LotSize` e períodos do indicador.
3. Inicie a estratégia; ele assinará velas, calculará os indicadores e executará negociações de acordo com a lógica descrita.
