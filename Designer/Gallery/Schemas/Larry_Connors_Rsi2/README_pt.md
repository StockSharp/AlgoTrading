# Diagrama da estratégia RSI-2 de Larry Connors
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O RSI-2 de Larry Connors compra o pânico e vende a euforia, mas apenas no sentido permitido pela média lenta: um RSI de dois períodos marca o extremo, uma SMA de 50 define a direção e uma SMA de 5 marca a hora de sair. O original opera candles de quatro horas; este diagrama usa candles de cinco minutos para acompanhar o histórico intradiário incluído.

![schema](schema.svg)

## Visão geral da estratégia

- O RSI de comprimento dois reage a um único candle, portanto uma leitura abaixo de 6 ou acima de 95 marca uma rajada curta de vendas ou de compras, não um estado duradouro.
- A SMA lenta funciona como filtro de direção: compras somente acima dela e vendas somente abaixo, o que mantém o diagrama do lado do movimento maior.
- A posição é aberta apenas a partir do zero e a SMA rápida a encerra assim que o preço volta a cruzar essa média, de modo que as operações costumam durar um ou dois candles.
- O bloco de proteção troca o stop e o alvo em pips do original por distâncias percentuais, já que o tamanho do pip não pode ser calculado a partir do passo de preço dentro de um diagrama.

## Regras de entrada e saída

- **Entrada comprada**: O RSI(2) está abaixo do nível de entrada comprada, o fechamento está acima da SMA lenta e a posição está zerada. A ordem compra o volume compartilhado a mercado e abre a compra.
- **Entrada vendida**: O RSI(2) está acima do nível de entrada vendida, o fechamento está abaixo da SMA lenta e a posição está zerada. A ordem vende o volume compartilhado a mercado e abre a venda.
- **Saída**: A compra é encerrada quando o fechamento volta acima da SMA rápida e a venda quando cai abaixo dela; o stop de 1% ou o alvo de 2% encerram a posição antes, se o preço chegar lá primeiro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Length | 2 | Período de suavização do índice de força relativa; dois candles por concepção. |
| Fast SMA Length | 5 | Período da SMA rápida que determina a saída. |
| Slow SMA Length | 50 | Período da SMA lenta que decide o lado permitido. |
| RSI Long Entry | 6 | Nível de RSI abaixo do qual uma compra é permitida. |
| RSI Short Entry | 95 | Nível de RSI acima do qual uma venda é permitida. |
| Take Profit, % | 2 | Distância do alvo em relação ao preço de entrada, em porcentagem. |
| Stop Loss, % | 1 | Distância do stop em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o RSI, as duas médias móveis e um conversor que lê o preço de fechamento de cada candle finalizado.
- Seis blocos de comparação carregam as regras: dois medem o RSI contra os níveis de entrada, dois o fechamento contra a SMA lenta e dois o fechamento contra a SMA rápida.
- Os dois E de entrada também recebem a verificação de posição zerada, e os blocos de entrada estão configurados para abrir posição, de modo que um sinal nunca aumenta uma operação em andamento.
- Os blocos de saída estão configurados para fechar posição e só agem quando existe posição do lado oposto; todas as operações próprias entram no bloco de proteção para que stop e alvo sigam a posição real.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
