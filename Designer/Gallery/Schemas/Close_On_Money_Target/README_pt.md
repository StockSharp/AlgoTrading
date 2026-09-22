# Diagrama de fechamento por meta monetária
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama adiciona uma saída monetária de emergência à direção SMA(10)/SMA(30). As entradas são limites pendentes para que Mass order cancellation tenha ordens reais a remover antes de ClosePosition ao atingir lucro ou perda não realizada.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam as SMAs; Greater e Less avaliam o estado em cada candle, não um cruzamento.
- Estado altista com Position <= 0 coloca compra no fechamento; baixista com Position >= 0 coloca venda.
- O volume é abs(Position) mais volume base, preservando a reversão numa ordem líquida.
- P&L change compara resultado não realizado com +300 e -150 na moeda da conta.
- Qualquer limite aciona Mass order cancellation e ClosePosition a mercado simultaneamente.

## Regras de entrada e saída

- **Entrada comprada**: SMA rápida acima da lenta e Position zerada ou vendida: a compra limitada cobre o short e deixa uma unidade base long.
- **Entrada vendida**: SMA rápida abaixo da lenta e Position zerada ou comprada: a venda limitada cobre o long e deixa uma unidade base short.
- **Saída**: PnLUnreal >= 300 ou <= -150 cancela todas as ordens ativas e fecha Position a mercado. Com P&L de volta a zero, a estratégia pode negociar novamente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado usado pelas duas médias simples e decisões de entrada. |
| Fast SMA Length | 10 | Número de valores na SimpleMovingAverage rápida. |
| Slow SMA Length | 30 | Número de valores na SimpleMovingAverage lenta. |
| Base Volume | 1 | Tamanho da posição após entrada zerada ou reversão líquida. |
| Profit Target, money | 300 | Lucro não realizado em moeda da conta que aciona liquidação. |
| Loss Limit, money | -150 | Limite de perda não realizada em moeda da conta; deve ser negativo. |

## Detalhes do diagrama

- RequestCloseAll nunca é chamado no C#; o caminho ativo só negocia estado SMA a mercado. O diagrama implementa a intenção monetária anunciada.
- Os parâmetros fonte são níveis de equity e valem zero por padrão; aqui são usados PnLUnreal, +300 e -150.
- Limites no fechamento substituem entradas a mercado para dar função a Mass order cancellation; ajuste ao passo fica desligado no replay.
- O código chamaria Stop após liquidar. O diagrama permanece ativo para mostrar ciclos repetidos no mês.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
