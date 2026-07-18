# Estratégia de modelo de rede neural
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o comportamento do modelo de consultor especialista MQL5 que alimenta recursos RSI e MACD em uma rede neural. Como StockSharp não é fornecido com o carregador de rede personalizado do projeto original, a estratégia substitui a rede caixa preta por um modelo de pontuação determinístico, mantendo a mesma estrutura de mercado e controles de risco. O objetivo é capturar o impulso quando RSI e MACD concordam com a direção e o movimento projetado é grande o suficiente para justificar uma negociação.

## Indicadores e dados
- **Índice de força relativa (RSI, 12 períodos)** calculado no fechamento da vela, refletindo a entrada de preço típica original.
- **Moving Average Convergence Divergence (MACD 12/48/12)** usado como histograma de momentum e proxy de confiança.
- **Prazo** configurável; o padrão são velas de 5 minutos para corresponder ao especialista de origem.

## Lógica de negociação
1. Em cada vela finalizada, a estratégia atualiza filas contínuas de valores de histograma RSI e MACD com a janela controlada por `BarsToPattern`.
2. O desvio RSI de 50 e o desvio do histograma MACD de sua média móvel são combinados em uma pontuação de confiança usando uma tangente hiperbólica para emular a função de esmagamento da rede.
3. Se a confiança absoluta exceder `TradeLevel` e o movimento projetado convertido em pontos for superior a `MinTargetPoints`, a estratégia emite uma ordem de mercado na direção sugerida pela pontuação.
4. Um take-profit dinâmico igual ao movimento projetado multiplicado por `ProfitMultiply` e limitado por `MaxTakeProfitPoints` é armazenado para tratamento manual de saída. Um stop loss simétrico em pontos reflete o comportamento original.
5. Enquanto uma posição está aberta, a estratégia verifica cada vela finalizada: se o preço atingir o stop ou alvo armazenado, ela fecha a posição no mercado e redefine o estado interno.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `BarsToPattern` | Número de velas armazenadas na janela contínua usada para calcular as estatísticas RSI e MACD. |
| `TradeLevel` | Confiança mínima (0-1) necessária para abrir uma posição. |
| `ProfitMultiply` | Multiplicador aplicado ao movimento projetado antes de limitá-lo com `MaxTakeProfitPoints`. |
| `MinTargetPoints` | Número mínimo de preços exigidos na projeção para entrar em uma negociação. |
| `MaxTakeProfitPoints` | Distância máxima, em pontos, permitida para o take-profit. |
| `StopLossPoints` | Distância, em pontos, do stop de proteção em relação ao preço de entrada. |
| `TradeVolume` | Volume enviado com cada ordem de mercado. |
| `CandleType` | Tipo de dados da vela ou período de tempo para assinatura. |

## Notas
- O modelo de confiança é intencionalmente determinístico para manter o comportamento transparente enquanto preserva a estrutura da abordagem original da rede neural.
- Os níveis de take-profit e stop-loss são gerenciados manualmente para que cada negociação mantenha suas próprias metas dinâmicas, semelhante à forma como a versão MQL5 usa a saída da rede.
- A estratégia só avalia novas entradas quando nenhuma posição está aberta, replicando a restrição de posição única do consultor especialista de origem.
