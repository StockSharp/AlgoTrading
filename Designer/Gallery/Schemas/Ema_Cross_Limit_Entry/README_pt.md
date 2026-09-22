# Entrada limitada acompanhada por cruzamento de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama preserva o cruzamento EMA(12)/EMA(26) e a confirmação Momentum(10) de Franks4HourLimitOrdersStrategy, mas explicita a execução: o limite nasce no fechamento, acompanha fechamentos seguintes com Order replacing e é cancelado no cruzamento oposto.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados alimentam duas EMAs e Momentum; Crossing emite apenas quando a ordem das médias realmente muda.
- O cruzamento altista exige Momentum positivo e Position <= 0; o baixista, Momentum negativo e Position >= 0.
- Order registering coloca o primeiro limite no fechamento do candle de sinal sem ajuste ao passo de preço.
- Combination guarda a ordem mais recente devolvida por Order replacing para atualizar e cancelar o objeto vivo.
- A substituição só ocorre enquanto EMA e Momentum continuam validando o lado.

## Regras de entrada e saída

- **Entrada comprada**: EMA(12) cruza acima da EMA(26), Momentum é positivo e Position está zerada ou vendida. A compra limitada usa abs(Position)+1 para fechar o short e abrir long numa reversão líquida.
- **Entrada vendida**: EMA(12) cruza abaixo da EMA(26), Momentum é negativo e Position está zerada ou comprada. A venda limitada usa o mesmo tamanho de reversão líquida.
- **Saída**: O cruzamento oposto cancela o limite pendente e pode enviar o contrário. A posição também recebe stop 1% e alvo 3% adicionados pelo diagrama; uma saída protetora cancela referências pendentes.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado: cinco minutos na galeria e quatro horas por padrão no C#. |
| Fast EMA Length | 12 | Número de valores na ExponentialMovingAverage rápida. |
| Slow EMA Length | 26 | Número de valores na ExponentialMovingAverage lenta. |
| Momentum Length | 10 | Número de valores do Momentum cujo sinal confirma o cruzamento. |

## Detalhes do diagrama

- O C# entra a mercado e não gerencia pendentes; registro, substituição e cancelamento são a adaptação de execução demonstrada.
- O limite aberto segue cada novo fechamento apenas enquanto ordem das EMAs, sinal do Momentum e lado da Position mantêm o setup.
- O padrão do código é quatro horas. O exemplo usa cinco minutos porque um mês H4 mal forma EMA(26); o parâmetro permite 04:00:00.
- Volume base 1 e Position protection com stop 1% / alvo 3% são adições fixas, não parâmetros do construtor.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
