# Diagrama de estratégia de ordens limitadas por pivôs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama transforma suportes e resistências clássicos de pivô em um par persistente de ordens limitadas. Um dia móvel de candles finalizados fornece a faixa, uma curta janela à meia-noite agenda o ciclo de vida e a referência atual de cada ordem percorre registro, substituição, cancelamento e gráfico.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam Highest e Lowest com janela de 288 barras, equivalente a um dia móvel; o fechamento mais recente completa o cálculo.
- As fórmulas calculam P = (H + L + C) / 3, R1 = 2P − L e S1 = 2P − H sem encadear uma fórmula em outra.
- Working time emite o pulso diário perto da meia-noite. Um estado retido permite que o primeiro pulso com níveis prontos e posição zerada registre apenas um par.
- Order registering coloca uma compra limitada em S1 e uma venda limitada em R1, mantendo exatamente o preço calculado.
- Combination guarda a referência mais recente. Pulsos posteriores acionam Order replacing e cada resultado retorna ao mesmo barramento.
- Position protection coloca um stop de 1% após uma execução. Sua execução aciona Order cancellation, enquanto a ordem no pivô oposto compensa naturalmente a posição.

## Regras de entrada e saída

- **Entrada comprada**: Depois de formada a faixa de 288 barras, o primeiro pulso válido coloca uma compra limitada em S1 com o volume configurado. O toque no suporte executa a entrada e a venda em R1 pode servir como saída oposta.
- **Entrada vendida**: O mesmo pulso coloca uma venda limitada em R1. O toque na resistência abre a posição vendida e a compra em S1 oferece a saída limitada oposta.
- **Saída**: A ordem no pivô oposto pode zerar a posição no outro extremo da faixa. Em paralelo, Position protection fecha um movimento adverso de 1% e cancela ordens ainda ativas após a execução.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Time frame dos candles finalizados usados na faixa diária móvel, níveis de pivô, tempo das ordens e proteção. |
| Order Volume | 1 | Quantidade de cada ordem limitada de compra e venda. |
| Stop Loss, % | 1 | Distância adversa da execução em que Position protection encerra, em porcentagem. |

## Detalhes do diagrama

- A janela Highest/Lowest de 288 barras aproxima um dia móvel e evita depender de uma assinatura diária separada.
- R1 e S1 são expandidos diretamente em H, L e C, preservando as equações e produzindo ambos no mesmo nível de processamento.
- Uma variável flag registra que o par inicial já foi armado e evita novos registros em cada candle.
- Ordens registradas e substituídas entram em barramentos Combination<Order>; cada saída de substituição volta ao barramento e mantém a referência atual.
- OnlineOnly = false e ShrinkPrice = false permitem replay histórico e preservam os preços calculados.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
