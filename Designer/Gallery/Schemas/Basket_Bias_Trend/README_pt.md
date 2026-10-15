# Diagrama de viés SMMA para um único instrumento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Apesar do nome histórico da galeria, isto não é uma cesta. O diagrama segue o VectorStrategy atual: um instrumento, médias suavizadas em candles de quatro horas, reversão líquida e saída por PnL flutuante em dinheiro absoluto.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de quatro horas alimentam SMMA(3) e SMMA(7); a ordem delas define viés altista ou baixista.
- Um Flag único arma N values e bloqueia os primeiros oito candles finalizados.
- O viés altista compra com Position <= 0 e o baixista vende com Position >= 0.
- O volume é abs(Position) mais volume base, unindo fechamento e abertura numa reversão líquida.
- P&L change fecha a exposição ao atingir +5000 ou -300000 na moeda da conta.

## Regras de entrada e saída

- **Entrada comprada**: Após o aquecimento, a SMMA rápida está acima da lenta e Position está zerada ou vendida. Modify position compra a mercado para fechar o short e deixar uma unidade base long.
- **Entrada vendida**: Após o aquecimento, a SMMA rápida está abaixo da lenta e Position está zerada ou comprada. Modify position vende a mercado para fechar o long e deixar uma unidade base short.
- **Saída**: O viés oposto inverte diretamente a posição. Além disso, PnL não realizado >= 5000 ou <= -300000 aciona ClosePosition a mercado; se o viés persistir, outro candle pode reentrar.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 04:00:00 | Intervalo finalizado para as duas médias suavizadas e decisões de posição. |
| Fast SMMA Length | 3 | Número de valores na SmoothedMovingAverage rápida. |
| Slow SMMA Length | 7 | Número de valores na SmoothedMovingAverage lenta. |
| MA Shift Warmup | 8 | Candles iniciais finalizados bloqueados antes de habilitar entradas. |
| Base Volume | 1 | Tamanho mantido após entrada zerada ou reversão líquida. |
| Profit Target, money | 5000 | Lucro não realizado em moeda da conta que aciona ClosePosition. |
| Loss Limit, money | -300000 | Limite de perda não realizada em moeda da conta; deve permanecer negativo. |

## Detalhes do diagrama

- GetWorkingSecurities retorna apenas (Security, CandleType); por isso não há Index, Sync, segundo instrumento nem confirmação de cesta.
- ProfitPercent 0,5 e LossPercent 30 viram +5000 e -300000 com o saldo inicial de teste de 1.000.000.
- Designer expõe PnL não realizado em dinheiro, mas não o saldo inicial; os percentuais tornam-se valores explícitos.
- O aquecimento conta os primeiros oito candles finalizados como processedBars; SMMA(7) já está formada antes da negociação.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
