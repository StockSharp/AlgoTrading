# Estratégia Exp de Ajuste Fino de Velas MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do especialista MetaTrader 5 `Exp_FineTuningMACandle.mq5`, que opera com base na cor do indicador *Fine Tuning MA Candle*.
- Projetada para a API de alto nível do StockSharp: subscreve uma única série de velas, obtém valores do indicador via `BindEx` e encaminha todas as ordens pelos métodos auxiliares da `Strategy`.
- Implementa as mesmas permissões de entrada e fechamentos condicionais que o especialista original, respeitando o modelo de execução assíncrona do StockSharp.

## Indicador Fine Tuning MA Candle
- O indicador constrói velas OHLC sintéticas aplicando um esquema de ponderação em três estágios às últimas `Length` velas da série de preços.
  - `Rank1`, `Rank2` e `Rank3` controlam a curvatura das rampas de ponderação, enquanto `Shift1`, `Shift2` e `Shift3` mesclam as rampas com um componente plano.
  - A ponderação é simétrica: a primeira metade da janela acelera em direção ao centro, a segunda metade desacelera afastando-se dele.
  - Após a normalização, as quatro somas ponderadas produzem preços suavizados de abertura, máximo, mínimo e fechamento.
- Quando a abertura e o fechamento suavizados diferem em menos de `GapPoints` (convertido para o passo de preço do instrumento), a abertura é substituída pelo fechamento sintético anterior para eliminar lacunas de preço.
- A vela é colorida **2** (altista) quando `Open < Close`, **0** (baixista) quando `Open > Close`, e **1** quando são iguais. Apenas o fluxo de cor é usado para decisões de trading.
- `PriceShiftPoints` desloca verticalmente cada vela sintética por um número configurável de passos de preço.

## Regras de trading
- Os sinais são gerados apenas em velas completadas. A estratégia armazena as cores do indicador e avalia a vela localizada `SignalBar` passos atrás da última finalizada.
- **Rotação altista (a cor muda para 2):**
  - As posições vendidas existentes são fechadas se `SellPosClose` estiver habilitado.
  - Assim que a posição estiver plana, e se `BuyPosOpen` estiver permitido, uma ordem de mercado comprada por `Volume` lotes é enviada. Se um vendido teve que ser fechado primeiro, a entrada comprada é enfileirada e disparada assim que a posição retorna a zero.
- **Rotação baixista (a cor muda para 0):**
  - As posições compradas existentes são fechadas se `BuyPosClose` estiver habilitado.
  - Uma vez plana, e se `SellPosOpen` estiver permitido, uma ordem de mercado vendida por `Volume` lotes é enviada. Entradas pendentes são usadas da mesma forma que para sinais comprados.
- A cor neutra (1) não aciona nenhuma ação.
- As ordens nunca são empilhadas: a estratégia abre no máximo uma posição por vez e aguarda o fechamento das posições ativas antes de reverter.

## Gestão de risco
- `StopLossPoints` e `TakeProfitPoints` representam distâncias em passos de preço. Após o preenchimento de uma nova posição, a estratégia registra ordens protetoras de stop e alvo usando o preço de preenchimento real reportado em `OnNewMyTrade`.
- Ordens protetoras são canceladas automaticamente quando a posição retorna a zero ou quando uma nova ordem é enfileirada.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Tipo de dados/período das velas usadas para cálculos do indicador. |
| `Length` | Número de velas processadas pela janela ponderada do indicador. |
| `Rank1`, `Rank2`, `Rank3` | Coeficientes de potência que moldam os três estágios de ponderação. |
| `Shift1`, `Shift2`, `Shift3` | Fatores de mistura (0–1) que combinam os estágios de ponderação com um componente plano. |
| `GapPoints` | Diferença máxima entre abertura e fechamento sintéticos suprimida copiando o fechamento anterior. Expressa em passos de preço. |
| `SignalBar` | Quantas velas fechadas ignorar antes de ler a cor do indicador. `1` significa "usar a última vela completada". |
| `BuyPosOpen` / `SellPosOpen` | Permitir abertura de posições compradas/vendidas. |
| `BuyPosClose` / `SellPosClose` | Permitir fechamento de posições compradas/vendidas quando a cor oposta aparecer. |
| `StopLossPoints` | Distância do preço de preenchimento ao stop de proteção. Defina como `0` para desativar. |
| `TakeProfitPoints` | Distância do preço de preenchimento ao alvo de lucro. Defina como `0` para desativar. |
| `PriceShiftPoints` | Deslocamento vertical aplicado às velas sintéticas, expresso em passos de preço. |

## Notas de implementação
- Usa `BindEx` porque o indicador personalizado retorna um objeto de valor complexo que expõe o OHLC sintético e a cor simultaneamente.
- Mantém apenas um pequeno histórico de valores de cor (`SignalBar + 2` entradas) para detectar mudanças de cor sem armazenar grandes buffers.
- Reversões de entrada respeitam o modelo de execução assíncrona aguardando que a posição fique plana antes de enviar a ordem do lado oposto.
