# Diagrama de escada de desvio da EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama une o sinal de reversão EMA(30)/StandardDeviation(14) do C# à ideia de grade do README. Um desvio 1,5σ arma limite próximo negociável e outro pendente em 2,5σ; a saída ocorre no retorno fonte EMA ± 0,5σ.

![schema](schema.svg)

## Visão geral da estratégia

- Candles replay finalizados de cinco minutos atualizam EMA, desvio e um fechamento sincronizado após os dois indicadores.
- Cruzar abaixo de EMA − 1,5σ registra compras em −1,5σ e −2,5σ com Position <= 0; acima é simétrico com Position >= 0.
- O degrau próximo normalmente executa de imediato e o distante espera movimento maior.
- Long fecha com Close > EMA + 0,5σ e short com Close < EMA − 0,5σ, como no código.
- Toda saída cancela os dois limites distantes e sinal oposto remove o degrau antigo da outra direção.

## Regras de entrada e saída

- **Entrada comprada**: Ao cruzar abaixo de EMA − 1,5σ com Position <= 0, registra compras de uma unidade em EMA − 1,5σ e EMA − 2,5σ.
- **Entrada vendida**: Ao cruzar acima de EMA + 1,5σ com Position >= 0, registra vendas de uma unidade em EMA + 1,5σ e EMA + 2,5σ.
- **Saída**: Close > EMA + 0,5σ fecha long e Close < EMA − 0,5σ fecha short por ClosePosition, que calcula todo o volume atual.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado; cinco minutos adapta as quatro horas do C#. |
| EMA Length | 30 | Período da EMA central e parâmetro C# real. |
| Standard Deviation Length | 14 | Período da largura; 14 é literal no C#. |
| Near Entry Deviation | 1.5σ | Multiplicador de entrada fonte, literal 1,5. |
| Far Grid Deviation | 2.5σ | Segundo rango pendente vindo do README. |
| Mean-Reversion Exit Deviation | 0.5σ | Limite de retorno fonte, literal 0,5. |
| Volume per Rung | 1 | Volume independente de cada degrau. |

## Detalhes do diagrama

- Somente EmaLength e CandleType são StrategyParam em C#; período 14 e multiplicadores 1,5/0,5 são literais expostos para estudo.
- C# usa quatro horas; cinco minutos é adaptação replay para gerar eventos suficientes no mês.
- O C# executável possui só o limiar 1,5σ apesar de Three Level Grid. O rango 2,5σ vem expressamente do README.
- A fonte reverte com duas ordens a mercado. Aqui o limite próximo pode zerar primeiro e o distante completar a reversão depois.
- Crossing cria uma escada por excursão e o cancelamento evita posição futura sem gestão por ordem restante.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
