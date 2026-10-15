# Diagrama de sessão de limites com RSI e Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama combina RSI(14), Momentum(14), filtro Working time de dia inteiro e limites pendentes gerenciados. Sobrevenda com impulso fraco coloca compra abaixo da abertura; sobrecompra com impulso forte coloca venda acima, e o desaparecimento do sinal cancela explicitamente a ordem.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam RSI, Momentum, abertura para entrada e fechamento para avaliar a proteção.
- Working time aceita candles de 00:00 a 23:59, igual à sessão efetivamente integral da fonte, mantendo o bloco configurável visível.
- RSI abaixo de 30, Momentum abaixo de 1 e Position <= 0 permitem compra; RSI acima de 70, Momentum acima de 1 e Position >= 0 permitem venda.
- Flags de disparo único mantêm no máximo uma ordem por episódio e cancelam ordens próprias vencidas ou opostas.
- Um limite executado recebe lucro absoluto 35 e perda 8, com o fechamento ligado à entrada de preço da proteção.

## Regras de entrada e saída

- **Entrada comprada**: Na sessão, RSI < 30, Momentum < 1 e Position <= 0 registram compra de uma unidade em OpenPrice − 25, cancelando antes qualquer venda ativa.
- **Entrada vendida**: Na sessão, RSI > 70, Momentum > 1 e Position >= 0 registram venda de uma unidade em OpenPrice + 25, cancelando antes qualquer compra ativa.
- **Saída**: A proteção fecha com ganho absoluto 35 ou perda 8. A compra pendente é cancelada quando RSI, Momentum ou posição deixam de ser válidos; a venda é simétrica.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado; cinco minutos é adaptação replay e C# usa quinze por padrão. |
| RSI Period | 14 | Quantidade de valores do RelativeStrengthIndex. |
| Momentum Period | 14 | Quantidade de valores do Momentum. |
| Session Start | 00:00:00 | Início da janela Working time. |
| Session End | 23:59:00 | Fim da janela; 23:59 preserva o dia inteiro. |
| RSI Buy Threshold | 30 | RSI deve ficar abaixo para comprar. |
| RSI Sell Threshold | 70 | RSI deve ficar acima para vender. |
| Momentum Threshold | 1 | Momentum deve ficar abaixo para comprar e acima para vender. |
| Limit Offset, price units | 25 | Distância absoluta subtraída ou somada a OpenPrice no replay. |
| Order Volume | 1 | Volume de cada limite pendente. |
| Take Profit, price units | 35 | Distância favorável absoluta desde a execução. |
| Stop Loss, price units | 8 | Distância adversa absoluta desde a execução. |

## Detalhes do diagrama

- C# usa candles de 15 minutos por padrão. O diagrama usa cinco minutos no replay para mostrar ciclos suficientes; os períodos continuam 14, portanto é uma adaptação explícita de amostragem.
- A fonte desloca 5 × PriceStep. Sem consumir esse passo em um bloco, o diagrama usa 25 unidades absolutas; é ajuste da galeria, não o padrão da fonte.
- Os limites são calculados de OpenPrice como em ProcessCandle. ClosePrice fica separado e apenas atualiza Position protection.
- As distâncias da fonte são 35 × PriceStep e 8 × PriceStep. O diagrama mantém 35 e 8 como unidades absolutas, não percentuais.
- Os flags modelam a verificação de ordem ativa e reiniciam quando RSI, Momentum ou condição de posição ficam inválidos.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
