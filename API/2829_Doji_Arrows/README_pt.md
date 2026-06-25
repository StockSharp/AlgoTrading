# Estratégia de Setas Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Conceito
A estratégia de Setas Doji converte o expert advisor original "Doji Arrows" do MetaTrader para a API de alto nível do StockSharp. A ideia é aguardar uma vela doji genuína e então operar um rompimento do seu range. Uma vela doji representa indecisão, portanto, um fechamento acima da máxima do doji indica força altista enquanto um fechamento abaixo da mínima do doji indica controle baixista.

1. A estratégia processa apenas velas completadas da assinatura `CandleType` configurada.
2. A vela anterior é analisada para determinar se é um doji. A vela é classificada como doji quando a diferença absoluta entre a abertura e o fechamento é menor ou igual a `DojiBodyPoints` multiplicado pelo passo de preço do instrumento. Se o parâmetro for definido como `0`, um único passo de preço é usado como tolerância, o que corresponde à verificação de igualdade estrita na versão MQL5.
3. Quando a próxima vela fecha acima da máxima do doji, a estratégia envia uma ordem de compra a mercado. Quando a próxima vela fecha abaixo da mínima do doji, uma ordem de venda a mercado é emitida. Posições opostas existentes são niveladas automaticamente pelo volume da ordem de mercado.

Esta sequência espelha o expert advisor original que reagia uma vez na abertura de cada nova barra.

## Gerenciamento de Risco
A implementação convertida mantém o comportamento protetor do script MQL:

- **Stop loss**: `StopLossPoints` controla quão longe, em passos de preço, o stop loss inicial é colocado do preço de entrada. Defina como zero para desabilitar o stop fixo.
- **Take profit**: `TakeProfitPoints` define a distância até o objetivo de lucro em passos de preço. Defina como zero para pular o objetivo.
- **Trailing stop**: `TrailingStopPoints` e `TrailingStepPoints` reproduzem o mecanismo de trailing. Uma vez que a operação ganha mais de `TrailingStopPoints + TrailingStepPoints`, o nível de stop é puxado para `TrailingStopPoints` do último fechamento (fechamento mais alto para comprado, fechamento mais baixo para vendido). O trailing é opcional e ativa apenas quando `TrailingStopPoints` é maior que zero.

Stops e objetivos são avaliados em cada vela terminada. Quando qualquer nível é violado (usando a máxima/mínima da vela), a estratégia sai da posição com uma ordem de mercado e reinicia o estado de proteção.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `StopLossPoints` | `30` | Distância do stop loss inicial em passos de preço. |
| `TakeProfitPoints` | `90` | Distância do take profit em passos de preço. |
| `TrailingStopPoints` | `15` | Distância usada pelo trailing stop em passos de preço. |
| `TrailingStepPoints` | `5` | Lucro adicional necessário antes de ajustar o trailing stop, em passos de preço. |
| `DojiBodyPoints` | `1` | Tamanho máximo permitido do corpo da vela anterior em passos de preço para tratá-la como um doji. `0` usa um passo de preço como tolerância. |
| `CandleType` | `1 hora` | Tipo de vela assinada para geração de sinais. |

## Notas de Implementação
- A estratégia assina velas através de `SubscribeCandles(CandleType).Bind(ProcessCandle)` e mantém apenas a última vela completada na memória.
- O passo de preço do instrumento é obtido via `Security?.PriceStep`. Quando não disponível, um valor de fallback de `1` é usado para que a estratégia possa operar igualmente com dados sintéticos ou históricos.
- Os níveis protetores são recalculados após cada entrada, e a lógica de trailing pode criar um stop mesmo quando o stop loss fixo está desabilitado (correspondendo ao comportamento MQL onde o trailing stop podia começar de zero).
- Todas as ações são executadas com ordens de mercado para manter alinhamento com o advisor original que dependia de execução imediata no mercado.

## Dicas de Uso
1. Configure as propriedades `Security`, `Portfolio` e `Volume` antes de iniciar a estratégia.
2. Ajuste os parâmetros baseados em pontos de acordo com o instrumento operado. Para instrumentos cotados com pips fracionários, aumente os valores para corresponder ao tamanho do tick do broker.
3. Combine a estratégia com controles de risco ou módulos de análise do StockSharp se for necessário dimensionamento de posição mais avançado, pois a conversão mantém a lógica de volume fixo do código original.
