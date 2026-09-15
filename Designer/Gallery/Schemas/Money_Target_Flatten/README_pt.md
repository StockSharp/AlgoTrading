# Diagrama da estratégia Money Target Flatten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma meta em dinheiro não olha para o preço: ela fecha a posição quando o dinheiro nela atinge um determinado valor. Este diagrama coloca essa regra sobre um motor simples de duas médias. O cruzamento decide quando estar no mercado; a meta de lucro e o limite de prejuízo decidem quando já é o bastante e, no momento em que um deles é alcançado, a posição é zerada e toda ordem ainda ativa é cancelada logo em seguida.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados alimentam uma média móvel exponencial rápida e uma lenta, e um bloco de cruzamento transforma o par em um único evento: verdadeiro quando a linha rápida sobe através da lenta, falso quando desce através dela.
- Um NOT lógico dá ao cruzamento para baixo um sinal próprio, de modo que cada direção é um portão por si só.
- O bloco de posição comparado com zero diz se o diagrama está zerado, comprado ou vendido, e cada portão é um AND lógico de um cruzamento e um estado de posição.
- Ambas as entradas são ordens a mercado de volume fixo e carregam a condição de abertura de posição, de modo que um cruzamento que chegue enquanto uma posição já está em curso não pode aumentá-la.
- O bloco Strategy P&L emite o resultado aberto da posição em curso em um ritmo próprio, várias vezes por hora em vez de uma vez por barra.
- Duas comparações confrontam esse número com a meta de lucro e com o limite de prejuízo, e cada limiar é uma variável disparada pelo mesmo valor de P&L, de modo que uma comparação sempre vê os dois operandos do mesmo instante.
- Um bloco Combination une as duas respostas em uma única linha de saída, que faz duas coisas ao mesmo tempo: Position modify fecha a posição a mercado e Mass order cancellation limpa o que ainda estiver ativo.
- Um cruzamento contrário a uma posição aberta também a fecha, de modo que o diagrama nunca permanece em uma operação contra a qual as médias se voltaram.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida cruza acima da lenta em um candle finalizado enquanto a posição está zerada: Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: A média rápida cruza abaixo da lenta em um candle finalizado enquanto a posição está zerada: Position modify vende o volume da ordem a mercado.
- **Saída**: Duas saídas independentes. A saída por dinheiro dispara assim que o resultado aberto da posição atinge a meta de lucro ou cai até o limite de prejuízo: o bloco Combination repassa o sinal, a posição é fechada a mercado e toda ordem remanescente é cancelada. Um cruzamento contrário à posição aberta também a fecha. O que vier primeiro, o diagrama fica zerado e espera, e o próximo cruzamento que o encontrar zerado abre a próxima posição, comprada ou vendida.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles sobre a qual ambas as médias são construídas. |
| Fast EMA Length | 12 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 26 | Período da média móvel exponencial lenta. |
| Volume | 1 | Tamanho da ordem, em lotes, enviado por cada entrada. |
| Profit To Close | 300 | Resultado aberto, em dinheiro, no qual a posição é fechada como ganho. |
| Loss To Close | -600 | Resultado aberto, em dinheiro, no qual a posição é fechada como perda; é escrito como número negativo porque é comparado diretamente com o resultado. |

## Detalhes do diagrama

- Os limiares são comparados com o resultado não realizado, o dinheiro da posição que está em curso, de modo que funcionam como um take-profit e um stop-loss em dinheiro sobre cada posição, uma a uma. As mesmas duas comparações ligadas à saída realizada transformam o par em um interruptor de via única: assim que a conta atinge o valor, a execução termina.
- A saída por dinheiro não está atrelada ao ritmo dos candles. Ela age na atualização do P&L, de modo que uma meta pode ser realizada no meio de uma barra em vez de no fechamento seguinte.
- Toda ordem que o diagrama envia é uma ordem a mercado, portanto, no histórico empacotado, a limpeza não encontra nada para cancelar. Ela está ligada porque uma saída por dinheiro que deixa ordens ativas para trás é apenas meia saída, e passa a importar no momento em que uma ordem em repouso entra no diagrama.
- Ambos os blocos de fechamento carregam a condição de fechamento de posição e, por isso, não precisam de lado nem de volume: o bloco lê a posição aberta e envia a ordem contrária exatamente nesse tamanho.
- O ritmo de cinco minutos é o que torna a camada de dinheiro visível. Em um candle muito mais longo, esse par de médias cruza apenas algumas poucas vezes por mês, as posições são poucas, e os limiares estão dimensionados para a oscilação que uma série de cinco minutos produz.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
