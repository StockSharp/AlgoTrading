# N Candles v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia busca um número configurável de candles consecutivos que fecham na mesma direção. Quando o comprimento da sequência é atingido, abre uma posição a mercado na direção do momentum detectado. A implementação espelha o expert advisor original do MetaTrader 5 "N- candles v2" e mantém a lógica focada em candles fechados para evitar sinais prematuros.

## Lógica da estratégia
1. Assinar a série de candles selecionada e aguardar barras completamente fechadas.
2. Categorizar cada candle como altista, baixista ou neutro (doji). Candles doji reiniciam a sequência.
3. Manter um contador acumulado de candles consecutivos com direção idêntica.
4. Quando o contador atingir o limiar `CandlesCount`, enviar uma ordem a mercado na mesma direção. O tamanho da ordem combina o `LotSize` solicitado com qualquer exposição contrária, de modo que a posição líquida final tenha o sinal e a quantidade pretendidos.
5. Armazenar o preço de entrada e inicializar os níveis de proteção usando as distâncias configuradas de stop-loss e take-profit.
6. A cada novo candle, atualizar o trailing stop (se habilitado) e sair de posições quando o preço tocar o stop-loss, trailing stop ou take-profit.

## Gestão de posição
- O stop-loss e take-profit iniciais são medidos em passos de preço (`Security.PriceStep`). Uma distância zero desativa o nível correspondente.
- O trailing stop é opcional. Quando habilitado, o stop é ajustado em `TrailingStopPips` assim que o preço se move favoravelmente pelo menos `TrailingStepPips` além da última localização do stop.
- Fechar uma posição remove todos os níveis em cache, de modo que uma nova sequência é necessária para a próxima entrada.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandlesCount` | Número de candles consecutivos que devem fechar na mesma direção antes de negociar. | 3 |
| `LotSize` | Tamanho da posição usado para cada entrada. A exposição contrária é fechada automaticamente. | 1 |
| `TakeProfitPips` | Distância take-profit em passos de preço a partir do preço de entrada. | 50 |
| `StopLossPips` | Distância stop-loss em passos de preço a partir do preço de entrada. | 50 |
| `TrailingStopPips` | Distância do trailing stop em passos de preço. Defina como 0 para desabilitar o trailing. | 10 |
| `TrailingStepPips` | Distância extra que o preço deve percorrer antes de ajustar o trailing stop. | 4 |
| `CandleType` | Período de candles usado para cálculos de sinal. | Candles de 1 hora |

## Notas
- A estratégia funciona com qualquer instrumento que forneça um `PriceStep` válido. Se o instrumento reportar zero, `1` é usado como fallback, correspondendo ao comportamento do script original.
- Os sinais são gerados apenas em candles concluídos, o que mantém o comportamento consistente entre backtesting e ambientes de trading ao vivo.
