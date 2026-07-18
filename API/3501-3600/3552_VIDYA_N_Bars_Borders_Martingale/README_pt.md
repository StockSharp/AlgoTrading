# Bordas de barras VIDYA N Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia original MetaTrader combina o indicador de canal "VIDYA N Bars Borders" com um módulo de dimensionamento de posição martingale. A porta StockSharp mantém a ideia de comprar quando o preço cair abaixo da banda inferior adaptativa e vender quando o preço subir acima da banda superior. O centro do canal é produzido por uma média móvel adaptativa (analógico VIDYA) e sua largura é controlada por um envelope Average True Range. Um bloco de gerenciamento de dinheiro aumenta o tamanho da negociação após perdas, ao mesmo tempo em que observa a posição máxima e os limites de exposição.

## Lógica de negociação
1. Assine as velas do período selecionado.
2. Calcule uma média móvel adaptativa de Kaufman como um substituto VIDYA e um canal ATR em torno dele.
3. Quando o fechamento de uma vela finalizada cruzar abaixo da banda inferior, abra ou reverta para uma posição longa (a menos que a bandeira `Reverse` esteja habilitada, caso em que uma posição curta é aberta).
4. Quando o fechamento cruzar acima da banda superior, abra ou reverta para uma posição curta (ou longa se `Reverse` for verdadeiro).
5. Imponha uma distância mínima de preço entre entradas consecutivas para evitar reinserções muito próximas do preenchimento anterior.
6. Se o lucro flutuante na posição aberta atingir a meta monetária especificada, achate tudo e aguarde o próximo sinal.
7. Após cada negociação fechada, o próximo volume base é redefinido para o tamanho inicial (após uma negociação lucrativa) ou multiplicado pelo índice de martingale (após uma negociação perdedora). O volume resultante é alinhado à etapa do instrumento e são aplicados limites de volume por negociação e total.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Candle Type` | Tipo de dados das velas a serem negociadas. |
| `CMO Period` | Janela de índice de eficiência para a média móvel adaptativa. |
| `EMA Period` | Período de suavização da média móvel adaptativa. |
| `ATR Period` | Número de barras para a meia largura do canal ATR. |
| `Profit Target` | Limite de lucro monetário que desencadeia uma saída total. |
| `Increase Ratio` | Multiplicador aplicado ao próximo volume de negociação após uma negociação perdida. |
| `Max Position Volume` | Teto rígido para um único volume de ordem/posição. |
| `Max Total Volume` | Limite superior da exposição total aberta pela estratégia. |
| `Max Positions` | Número máximo de posições simultâneas (a porta mantém uma posição líquida). |
| `Minimum Step` | Distância mínima entre duas entradas consecutivas, medida em pontos. |
| `Base Volume` | Tamanho inicial do pedido antes dos ajustes de martingale. |
| `Reverse Signals` | Inverte a interpretação longa/curta do rompimento do canal. |

## Notas de implementação
- StockSharp não inclui uma implementação direta do VIDYA. A estratégia usa `KaufmanAdaptiveMovingAverage` com eficiência configurável e janelas de suavização para imitar o comportamento adaptativo do VIDYA. Isso mantém a capacidade de resposta próxima ao indicador original, ao mesmo tempo em que depende de componentes integrados.
- Apenas uma posição líquida é gerenciada por vez. A versão MetaTrader enfileirou várias entradas pendentes; em StockSharp cada sinal abre uma nova posição ou reverte a atual. O dimensionamento Martingale é aplicado ao próximo tamanho de entrada em vez de adicionar novas camadas imediatamente.
- O alinhamento mínimo de passos e volumes depende dos metadados do instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`). Forneça esses valores ao configurar a estratégia para obter limites de execução precisos.
- O rastreamento de lucro é baseado na estratégia `PnL` e no último fechamento da vela, o que é suficiente para backtests de alto nível. Para negociação ao vivo, conecte a estratégia a um portfólio que atualize os valores de PnL realizados.

## Arquivos
- `CS/VidyaNBarsBordersMartingaleStrategy.cs` — Implementação da estratégia em C#.
