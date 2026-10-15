# Diagrama de estratégia de gestão de cotações de market maker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama transforma um sinal de reversão à média em um ciclo completo de cotação. Quando o preço sai da banda em torno de uma média lenta, ele coloca uma ordem limitada no seu lado do livro, acompanha o melhor preço por substituições, cancela ordens vencidas ou contraditas e só cruza o spread após o prazo da tentativa passiva.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de cinco minutos alimentam uma SimpleMovingAverage de 100 períodos; duas fórmulas colocam as bandas 0,8% acima e abaixo.
- Market depth fornece BestBid e BestAsk. Variáveis amostram os preços no relógio da vela para evitar desencontro com as atualizações do livro.
- O fechamento atual deve estar fora da banda e o anterior ainda dentro. Comparações de Position impedem aumentar uma posição no mesmo sentido.
- Order registering coloca compra limitada no BestBid amostrado ou venda limitada no BestAsk com Quote Volume comum.
- Combination mantém a referência atual. Order replacing devolve cada substituição ao fluxo e move a cotação quando o desvio relativo ultrapassa o limite.
- Order cancellation retira a cotação oposta no rompimento da outra banda e ordens vencidas após 12 velas. Se o preço continua fora e a posição está zerada, Modify position abre a mercado.
- Position protection fecha com lucro de 0,8% ou perda de 0,4%; a execução de saída aciona Mass order cancellation.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento cai abaixo da banda inferior depois de o fechamento anterior estar nela ou acima; Position não está comprado e há BestBid amostrado. Uma compra limitada é colocada e substituída quando o desvio supera 0,001. Após 12 velas, se Position continua zerada e o preço abaixo da banda, a cotação é cancelada e uma compra OpenPosition a mercado é enviada.
- **Entrada vendida**: O fechamento sobe acima da banda superior depois de o fechamento anterior estar nela ou abaixo; Position não está vendido e há BestAsk amostrado. Uma venda limitada é colocada e acompanhada por substituições. Após 12 velas, com Position zerada e preço acima da banda, ela é cancelada e uma venda OpenPosition a mercado é enviada.
- **Saída**: Todas as execuções de entrada vindas do registro, substituições ou alternativa a mercado chegam à Position protection. Fechamentos de vela controlam take-profit de 0,8% e stop-loss de 0,4%. Após uma execução protetora, Mass order cancellation remove as cotações restantes.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Período usado pela média, sinais, amostragem do livro, idade da cotação e proteção. |
| SMA Length | 100 | Comprimento da SimpleMovingAverage central. |
| Band Deviation | 0.008 | Meia largura relativa; 0,008 significa 0,8% de cada lado. |
| Quote Volume | 1 | Quantidade das cotações, substituições e entradas alternativas. |
| Re-quote Threshold | 0.001 | Distância relativa ao melhor preço que aciona uma substituição. |
| Quote Life, candles | 12 | Número de velas concluídas antes de a tentativa passiva vencer. |
| Take Profit, % | 0.8 | Distância favorável da execução de entrada, em percentual. |
| Stop Loss, % | 0.4 | Distância adversa da execução de entrada, em percentual. |

## Detalhes do diagrama

- Previous value guarda a vela anterior antes de converter seu fechamento; assim o setup é um evento de saída da banda, não uma condição repetida.
- BestBid, BestAsk e Position são retidos em variáveis e liberados pela vela concluída, dando o mesmo horário às comparações e ordens.
- Cada ordem registrada ou substituída entra em um barramento Combination<Order>, que alimenta conversão, substituição, cancelamento e gráfico com a ordem mais recente.
- O contador reinicia ao sair de uma banda, avança uma vez por vela e é limitado uma unidade acima do prazo; a igualdade com 12 produz um único evento de vencimento.
- Registro e substituição limitados preservam o preço recebido. A alternativa a mercado usa OpenPosition, e proteção e cancelamento em massa limpam o estado após a saída.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
