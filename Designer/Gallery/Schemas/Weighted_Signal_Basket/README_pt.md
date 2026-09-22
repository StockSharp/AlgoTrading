# Cesta ponderada de sinais com limites expirando
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama combina um voto da zona RSI e outro da posição perante EMA em uma pontuação de −3 a +3. Um cruzamento com posição zerada registra limite no close finalizado; Combination<Order> entrega essa ordem a N values por doze candles e a Order cancellation, e a execução inicia Position protection de 1,2%/0,8%.

![schema](schema.svg)

## Visão geral da estratégia

- RSI abaixo de 30 contribui +2, acima de 70 contribui −2 e a zona média contribui zero.
- Close acima da EMA(20) contribui +1 e abaixo −1; Formula soma os dois votos ponderados.
- Comparações atual e Previous value detectam novo cruzamento de +1 para long ou −1 para short, sempre com Position == 0.
- Compra e venda usam o close final e volume um; saídas Order convergem em Combination e MyTrade alimenta Position protection.
- N values conta doze candles finalizados após o registro e Order cancellation remove a ordem atual não executada.

## Regras de entrada e saída

- **Entrada comprada**: A pontuação sobe de menos de +1 para pelo menos +1 com posição zerada. Order registering põe compra limitada no close finalizado.
- **Entrada vendida**: A pontuação cai de mais de −1 para no máximo −1 com posição zerada. Order registering põe venda limitada no close finalizado.
- **Saída**: Entrada executada é protegida em +1,2% e −0,8% do fill. Limite pendente chega como objeto Order a Order cancellation depois de doze candles contados por N values.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado; cinco minutos adaptam o padrão C# de 60 minutos ao replay mensal. |
| RSI Length | 14 | Período RSI; C# usa 21 por padrão e o diagrama 14. |
| EMA Length | 20 | Período EMA; C# usa 50 por padrão e o diagrama 20. |
| RSI Weight | 2 | Peso do voto RSI sobrevendido ou sobrecomprado. |
| Trend Weight | 1 | Peso do voto do close perante EMA. |
| Replay Signal Threshold | 1 | Fronteira replay; o valor 2 do blueprint permanece alternativa documentada. |
| Cancel After N Candles | 12 | Candles finalizados antes de tentar cancelar entrada pendente. |
| Take Profit, % | 1.2 | Ganho percentual de Position protection. |
| Stop Loss, % | 0.8 | Perda percentual de Position protection. |
| Order Volume | 1 | Volume de cada entrada limitada. |

## Detalhes do diagrama

- O C# executável usa por padrão 60 minutos, RSI(21), EMA(50), comportamento com limiar 2 e cooldown de quatro candles; também pontua direção do candle e zonas RSI intermediárias. O diagrama usa 5 minutos, 14/20, dois votos e sem cooldown separado.
- O blueprint revisado propôs limiar 2. Com só dois votos, o replay de março não gerou ordens porque RSI sobrevendido normalmente coincidiu com preço abaixo da EMA e os votos se anulavam. Por isso o padrão replay transparente é 1; continua exposto para restaurar 2.
- O README vizinho descreve oito padrões, deslocamento pendente, expiração e proteção do expert original. O C# atual implementa três famílias de pontuação e entradas a mercado, sem blocos de expiração ou proteção.
- O limite no close substitui de propósito a entrada a mercado para dar ciclo real a Combination, N values e Order cancellation. O ajuste ao passo de preço é desativado porque o instrumento replay não o informa.
- Diferente de Position <= 0 / >= 0 no C#, o diagrama entra só zerado e não reverte. A proteção percentual 1,2/0,8 é educacional, não lógica do C#.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
