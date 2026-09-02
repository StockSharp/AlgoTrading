# Diagrama da estratégia de momento com candle cheio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um candle cheio abre em uma ponta da sua amplitude e fecha na outra: as sombras juntas ocupam no máximo uma pequena parte da distância entre máxima e mínima. Essa barra é um único empurrão ininterrupto, e o diagrama entra no sentido do corpo desde que uma média móvel exponencial concorde com a direção. A operação recebe um alvo fixo de uma fração de ponto percentual e nada além disso.

![schema](schema.svg)

## Visão geral da estratégia

- Conversores leem a abertura, a máxima, a mínima e o fechamento do candle finalizado, e dois blocos de fórmula medem quanto da amplitude as sombras ocupam.
- A medida de alta é a sombra superior mais a inferior de um candle que sobe, multiplicada por cem e comparada com a parcela de sombra aplicada à amplitude total; a medida de baixa é o espelho.
- Uma média móvel exponencial do preço de fechamento é o filtro de tendência: candles cheios de alta só são comprados acima dela e os de baixa só vendidos abaixo.
- Um bloco de proteção de posição encerra cada operação em um alvo fixo, a única saída que a estratégia original possui.

## Regras de entrada e saída

- **Entrada comprada**: A medida de sombras de alta está abaixo de zero, ou seja, o candle subiu e suas sombras ficaram dentro da parcela permitida da amplitude; o fechamento está acima da EMA e a posição ainda não está comprada. A ordem compra a constante de volume mais a venda em aberto, invertendo a venda e abrindo uma compra em uma única ordem.
- **Entrada vendida**: A medida de sombras de baixa está abaixo de zero, o fechamento está abaixo da EMA e a posição ainda não está vendida. A ordem vende a constante de volume mais a compra em aberto, invertendo a compra e abrindo uma venda em uma única ordem.
- **Saída**: O bloco de proteção realiza lucro a 0,3 por cento do preço de entrada, o mesmo número fixado no código original, e não há stop porque o original também não tem. Duas diferenças merecem atenção. O bloco de proteção acompanha o preço dentro da barra, enquanto o original só verifica o fechamento de um candle finalizado, então aqui o alvo dispara um pouco antes. E a pausa de quinze candles do original após cada operação ficou de fora: um contador de barras só se monta devolvendo um sinal ao diagrama, o que fecharia o grafo em um laço, de modo que o sinal de inversão é executado assim que aparece.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| EMA Length | 20 | Período da média móvel exponencial usada como filtro de tendência. |
| Shadow share, % | 10 | Maior parcela da amplitude do candle, em porcentagem, que as duas sombras juntas podem ocupar. |
| Take profit, % | 0.3 | Distância do alvo em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes; a ordem de inversão soma a isso o tamanho da posição que está sendo encerrada. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. A estratégia original usa candles de quinze minutos; aqui são cinco minutos, para que o padrão apareça com frequência suficiente no histórico incluído. |

## Detalhes do diagrama

- Cada fórmula subtrai o orçamento de sombra permitido das sombras reais, então um valor abaixo de zero significa candle de corpo cheio; a constante com a parcela de sombra alimenta as duas fórmulas.
- A direção não precisa de comparação própria: escrita para um candle que sobe, a medida de alta é sempre positiva em um candle que cai e também em um candle sem amplitude, então um valor abaixo de zero já significa que o candle subiu.
- O bloco de posição segue por dois caminhos: para as comparações com zero que protegem as entradas e para a fórmula de volume, que soma o módulo da posição à constante para que uma ordem a mercado encerre o lado contrário e abra o novo.
- Os dois blocos de entrada entregam suas próprias execuções ao bloco de proteção, que registra o alvo; o preço de fechamento entra no mesmo bloco como referência de preço.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
