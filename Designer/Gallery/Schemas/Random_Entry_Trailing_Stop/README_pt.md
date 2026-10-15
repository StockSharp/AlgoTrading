# Diagrama da estratégia de entrada aleatória com trailing stop pelo fluxo de negócios
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A maioria dos diagramas gasta seus blocos decidindo quando entrar. Este quase não gasta nada nisso e concentra tudo na saída. A entrada é um cara ou coroa: um bloco Random sorteia um número a cada candle fechado, e o lado do limiar em que ele cai decide se a posição será comprada ou vendida. O que acontece em seguida é o ponto do exemplo — um trailing stop reprecificado a cada negócio executado do fluxo, em vez de uma vez por barra.

![schema](schema.svg)

## Visão geral da estratégia

- Uma série de candles de cinco minutos é o relógio do diagrama. Cada candle fechado é um sorteio e uma tentativa de entrada; nada mais faz a lógica de entrada avançar.
- O bloco Random é acionado por esse candle e sorteia um número entre zero e um. Uma comparação com o limiar dá o lado comprado, e um NOT lógico da mesma resposta dá o lado vendido, de modo que uma única comparação serve às duas direções.
- A posição atual é sincronizada com o compasso do candle por uma variável que a retém em sua entrada e a libera no gatilho do candle; comparar esse valor retido com zero é a verificação de posição zerada.
- Duas portas lógicas AND combinam o cara ou coroa com a verificação de posição zerada. Ambas as entradas de cada porta chegam no compasso do candle, então cada porta é decidida exatamente uma vez por candle fechado.
- Os dois blocos de entrada carregam a condição de posição aberta, então uma porta que continue dizendo sim não consegue aumentar uma posição que já existe — o diagrama mantém uma posição por vez e nunca a inverte.
- O fluxo de ticks é assinado junto com os candles, e um conversor lê o preço de cada negócio executado.
- O Position protection recebe as execuções de entrada por meio de um Combination e esse preço de negócio em seu conector Price. O trailing está ligado, então cada negócio que leva a operação mais para o lucro arrasta o stop atrás de si, e a saída é decidida entre os candles, não sobre eles.
- O painel de gráfico desenha os candles, as duas ordens de entrada, a ordem de stop de proteção e cada execução, de modo que toda a vida de uma posição fica legível em um único painel.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle fechado, o número sorteado está abaixo do limiar e a posição retida é zero. A porta de compra é liberada e o Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: No mesmo candle fechado, o número sorteado é igual ou superior ao limiar — a comparação de compra invertida pelo NOT lógico — e a posição retida é zero. O Position modify vende o volume da ordem a mercado.
- **Saída**: Não há sinal de saída nem take-profit. O Position protection assume a operação a partir de sua primeira execução: coloca o stop a 0.5% do preço de entrada e, com o trailing ligado, o leva para cima atrás de uma posição comprada e para baixo atrás de uma vendida à medida que preços melhores são negociados. A posição é fechada por uma ordem a mercado no instante em que um negócio toca esse nível, o que deixa o próximo candle fechado livre para jogar a moeda de novo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles. Um candle fechado é um cara ou coroa e uma tentativa de entrada. |
| Coin Threshold | 0.5 | O valor com o qual o número sorteado é comparado. Em 0.5 as duas direções são igualmente prováveis; um valor menor torna as compras mais raras, e um maior as torna mais frequentes. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Trailing Stop, % | 0.5 | Distância do trailing stop, em percentual do preço de entrada. |

## Detalhes do diagrama

- O bloco Random sorteia da fonte aleatória da própria estratégia, e não de uma global, então repetir o mesmo histórico duas vezes dá a mesma sequência de sorteios e o mesmo conjunto de operações.
- O stop é escrito como um percentual do preço de entrada, e não como uma quantidade fixa de passos de preço. A mesma distância em unidades de preço significa uma coisa em um instrumento cotado perto de 65 000 e algo completamente diferente em outro cotado perto de 5; o percentual é a única forma que sobrevive aos dois.
- Alimentar o conector Price a partir do fluxo de negócios é o que torna a saída granular. Precificado pelo fechamento de um candle, o mesmo stop só seria testado doze vezes por hora.
- O stop acompanha continuamente: cada melhora no preço o move, sem nenhuma distância extra que o preço precise percorrer antes que o stop possa avançar de novo.
- Uma entrada é tentada a cada candle fechado enquanto a posição está zerada, então o diagrama normalmente está carregando algo e o stop de proteção sempre tem uma posição para administrar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
