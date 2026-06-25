# Estratégia Wajdyss Ichimoku Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um porte direto do especialista MetaTrader *Exp_wajdyss_Ichimoku_Candle_MMRec*. Ela recria o indicador "wajdyss Ichimoku candle"
calculando a linha base do Ichimoku (Kijun) e classificando cada vela concluída em um de quatro estados de cor. O sistema então procura uma reversão
nessas cores para desaparecer com o movimento mais recente. Quando a barra anterior estava acima do Kijun e a última barra de sinal cai abaixo dele,
o algoritmo fecha qualquer exposição vendida e abre uma operação comprada. A transição oposta muda para uma posição vendida. Um módulo adaptativo de
gestão de dinheiro replica a lógica MMRec original reduzindo o tamanho da posição após um número configurável de operações perdedoras na mesma direção.

A versão convertida usa a API de alto nível do StockSharp. As velas são fornecidas através de uma única chamada `SubscribeCandles`, e o nível Kijun é
calculado com os indicadores `Highest`/`Lowest`. As decisões de trading são avaliadas apenas em velas concluídas para manter o comportamento determinístico
nos modos em tempo real e histórico.

## Lógica de coloração de velas
Cada vela fechada recebe um índice de cor numérico que corresponde ao indicador MQL5 original:

| Cor | Condição | Significado |
|-----|----------|-------------|
| `0` | Fechamento abaixo do Kijun e corpo baixista | Forte sentimento baixista abaixo da linha base |
| `1` | Fechamento abaixo do Kijun mas corpo altista | Fraca reação altista abaixo da linha base |
| `2` | Fechamento acima do Kijun mas corpo baixista | Fraca reação baixista acima da linha base |
| `3` | Fechamento acima do Kijun e corpo altista | Forte continuação altista acima da linha base |

## Lógica de sinais
Os sinais são gerados em velas concluídas comparando a cor de duas barras históricas:

- **Configuração comprado**: a barra em `SignalBarShift + 1` tinha uma cor maior que `1` (preço acima do Kijun) e a barra em `SignalBarShift` tem
  uma cor abaixo de `2` (preço moveu-se abaixo do Kijun). A estratégia opcionalmente fecha qualquer posição vendida aberta e pode abrir um novo comprado.
- **Configuração vendido**: a barra em `SignalBarShift + 1` tinha uma cor abaixo de `2` (preço abaixo do Kijun) enquanto a barra em `SignalBarShift`
  imprime uma cor acima de `1` (preço moveu-se acima do Kijun). A estratégia opcionalmente fecha comprados existentes e pode entrar em uma posição vendida.

O parâmetro `SignalBarShift` corresponde à entrada `SignalBar` da versão MetaTrader. O valor padrão `1` significa que o sinal usa a última vela completamente
fechada e a anterior. Aumentar o deslocamento atrasa as entradas pelo número solicitado de barras.

## Gestão de dinheiro
O módulo MMRec mantém um histórico curto dos resultados de operações por direção. Se as últimas `LossTriggerCount` operações em uma direção foram todas
perdedoras, a estratégia muda para o tamanho de ordem reduzido (`ReducedVolume`). Após uma operação lucrativa, ou quando menos do que o número solicitado
de operações estão disponíveis, o volume padrão (`NormalVolume`) é restaurado. Isso espelha o comportamento de `BuyTradeMMRecounter` e `SellTradeMMRecounter`
da biblioteca MQL original.

## Gestão de risco
Os níveis protetores de stop-loss e take-profit são expressos em passos de preço. Quando uma posição comprada está aberta, a estratégia verifica se o mínimo
da vela atingiu `entrada - StopLossPoints * PriceStep` ou se o máximo tocou `entrada + TakeProfitPoints * PriceStep`. O lado vendido espelha a lógica. Os stops
são avaliados uma vez por vela concluída, semelhante ao EA fonte que dependia de ordens do lado do servidor com distância fixa.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Tipo de dados de velas (período) usado para o indicador | Velas de 1 hora |
| `KijunLength` | Lookback da linha base do Ichimoku | 26 |
| `SignalBarShift` | Número de barras fechadas a pular antes de avaliar a transição de cor | 1 |
| `BuyPosOpen` / `SellPosOpen` | Habilitar ou desabilitar a abertura de posições em cada direção | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir o fechamento de posições existentes no sinal oposto | `true` |
| `NormalVolume` | Volume de ordem padrão | `1` |
| `ReducedVolume` | Volume de ordem após o número configurado de perdas | `0.1` |
| `LossTriggerCount` | Número de operações perdedoras seguidas antes de reduzir o tamanho | `2` |
| `StopLossPoints` | Distância do stop em passos de preço (definir como `0` para desabilitar) | `1000` |
| `TakeProfitPoints` | Distância do take-profit em passos de preço (definir como `0` para desabilitar) | `2000` |

## Notas de uso
- A estratégia abre operações apenas quando a transição de cor indica exaustão e a direção relevante está habilitada.
- O escalonamento de volume requer que a plataforma relate resultados de operações; em backtests as saídas geradas pela estratégia atualizarão o histórico de perdas automaticamente.
- Se nenhum passo de preço for definido para o instrumento, as entradas de stop-loss e take-profit são ignoradas.
- Definir `SignalBarShift` como `0` imita uma reação imediata à última vela concluída, mas aumenta o risco de whipsaws.
