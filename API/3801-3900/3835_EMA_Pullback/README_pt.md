# EMA Estratégia de retrocesso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de pullback EMA é uma versão de alto nível do consultor especialista MetaTrader "Ema". Ele observa um par de médias móveis exponenciais (EMA) com períodos 5 e 10 calculados sobre os preços médios das velas. Quando aparece um cruzamento de alta ou baixa, a estratégia espera que o preço retorne ao extremo da vela anterior antes de entrar na direção do cruzamento. Os níveis fixos de take-profit e stop-loss medidos em pontos de preço gerenciam o risco quando a posição é aberta.

## Lógica de negociação
1. Assine a série de velas configurada (padrão: período de 5 minutos) e calcule duas EMAs no preço médio `(high + low) / 2`.
2. Detecte um cruzamento de alta quando o EMA rápida cruza acima do EMA lenta, ou um cruzamento de baixa quando o EMA rápida cruza abaixo do EMA lenta.
3. Armar uma entrada de pullback após ocorrer o cruzamento:
   - Para uma configuração longa, espere até que o preço de fechamento recue para a máxima da vela anterior menos o deslocamento `MoveBackPoints` enquanto o EMA rápida permanece acima do EMA lenta em pelo menos dois pontos de preço.
   - Para uma configuração curta, espere até que o preço de fechamento retorne ao mínimo da vela anterior mais o deslocamento `MoveBackPoints` enquanto o EMA lenta permanece acima do EMA rápida em pelo menos dois pontos de preço.
4. Quando a condição de pullback for satisfeita, envie uma ordem de mercado com o volume de negociação configurado.
5. Após a entrada, calcule os níveis estáticos de take-profit e stop-loss usando as configurações `TakeProfitPoints` e `StopLossPoints`, convertidas em compensações de preço absoluto do preço de entrada.
6. Monitore cada vela finalizada e feche a posição assim que o nível de take-profit ou stop-loss for tocado pela máxima/mínima da vela.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `TradeVolume` | `0.1` | Volume utilizado para cada ordem de mercado. |
| `FastLength` | `5` | Período do EMA rápida aplicado aos preços medianos. |
| `SlowLength` | `10` | Período de lentidão EMA aplicado aos preços medianos. |
| `MoveBackPoints` | `3` | Distância de pullback, em preços, medida a partir do extremo da vela anterior. |
| `TakeProfitPoints` | `5` | Distância de lucro, em faixas de preço. |
| `StopLossPoints` | `20` | Distância stop-loss, em faixas de preço. |
| `CandleType` | `5m` | Período usado para assinatura de velas e cálculos de indicadores. |

## Notas
- Apenas velas totalmente formadas são processadas para evitar sinais prematuros.
- A estratégia alinha automaticamente a propriedade `Strategy.Volume` com o parâmetro `TradeVolume` no início.
- Todos os cálculos dependem do instrumento `PriceStep` para converter distâncias baseadas em pontos em preços absolutos.
- A estratégia abre no máximo uma posição por vez e requer um novo cruzamento EMA antes de preparar outra negociação.
