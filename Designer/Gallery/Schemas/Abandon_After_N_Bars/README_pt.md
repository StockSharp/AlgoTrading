# Diagrama da estratégia Abandon After N Bars
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas médias móveis curtas e um RSI de dois períodos decidem quando abrir uma operação, mas o ponto deste diagrama é a regra que a encerra. Uma operação que não atingiu nem o alvo nem o stop dentro de um número fixo de candles é abandonada e fechada a mercado. A contagem é medida por um bloco N values e começa na execução que efetivamente abriu a posição, e não no sinal que a solicitou.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de cinco minutos finalizados alimentam uma média móvel exponencial rápida e uma lenta, um RSI de dois períodos e um conversor que extrai o preço de fechamento de cada candle.
- Duas comparações leem a tendência a partir das médias: rápida acima da lenta e rápida abaixo da lenta. Outras duas leem o momentum contra uma variável de limiar: RSI abaixo dele e RSI acima dele.
- O bloco Position é comparado com zero duas vezes, uma por igualdade e outra por desigualdade, de modo que o diagrama tem tanto um teste de posição zerada para as entradas quanto um teste de posição aberta para a regra de abandono.
- Cada entrada é um AND lógico de três sinais — tendência, momentum e posição zerada — e ambos os blocos de entrada estão configurados apenas para abrir, de modo que o diagrama mantém uma posição por vez e nunca a aumenta.
- O Position protection acompanha as execuções de entrada e administra a operação com um take-profit percentual e um stop-loss móvel (trailing) que segue o preço assim que ele se move a favor da posição.
- O Strategy trades informa cada execução própria. Um bloco Flag, reiniciado enquanto a posição está zerada, deixa passar apenas a primeira execução de uma operação, que é a execução que a abriu.
- Esse único pulso arma o bloco N values, que então conta os candles finalizados e emite um sinal quando a contagem é atingida.
- O sinal de abandono passa por um segundo AND com o teste de posição aberta antes de chegar a um bloco Position modify configurado para fechar, de modo que a contagem só pode encerrar uma operação que ainda esteja em andamento.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida está acima da lenta, o RSI está abaixo do seu limiar e a posição está zerada. A combinação compra a fraqueza dentro de uma tendência de alta de curto prazo. O Position modify compra o volume da ordem a mercado, apenas abrindo.
- **Entrada vendida**: A média rápida está abaixo da lenta, o RSI está acima do seu limiar e a posição está zerada — força dentro de uma tendência de baixa de curto prazo. O Position modify vende o volume da ordem a mercado, apenas abrindo.
- **Saída**: Há duas saídas independentes. O Position protection pode fechar a operação primeiro, com 1.2% de lucro ou por um trailing stop a 0.6% do melhor preço alcançado. Se nenhuma das duas ocorrer, a regra de abandono assume: doze candles finalizados após a execução de abertura, o bloco N values dispara, o teste de posição aberta confirma que ainda há algo a fechar e um bloco Position modify configurado para fechar zera o lado que estiver posicionado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame dos candles com que todo o diagrama trabalha; a contagem de abandono é medida nesses candles. |
| Fast EMA Length | 3 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 7 | Período da média móvel exponencial lenta; mantenha-o maior que o da rápida, ou o teste de tendência perde o sentido. |
| RSI Length | 2 | Período do RSI. Um período muito curto faz com que ele cruze o limiar com frequência, o que é justamente o que produz entradas frequentes. |
| RSI Threshold | 50 | Nível com o qual o RSI é comparado. Uma posição comprada é aberta abaixo dele e uma vendida, acima dele, portanto elevá-lo torna as entradas compradas mais frequentes e as vendidas mais raras. |
| Order Volume | 1 | Tamanho de cada ordem de entrada, em unidades do instrumento. |
| Take Profit, % | 1.2 | Distância do take-profit, em porcentagem do preço de entrada. |
| Trailing Stop, % | 0.6 | Distância do trailing stop, em porcentagem: o stop começa a essa distância da entrada e acompanha o melhor preço alcançado, sem nunca retroceder. |
| Bars Before Abandon | 12 | Quantos candles finalizados uma operação pode durar antes de ser abandonada e fechada a mercado. Reduza para abandonar mais rápido; aumente para deixar a saída por conta do take-profit e do trailing stop. |

## Detalhes do diagrama

- A contagem é iniciada por uma execução, e não por um sinal, de modo que o relógio mede há quanto tempo a operação realmente existe, e não há quanto tempo o diagrama a desejou.
- O bloco de execuções da estratégia também informa as saídas, o que de outra forma reiniciaria a contagem a cada execução de fechamento. O bloco Flag impede isso: ele só é reiniciado enquanto a posição está zerada, portanto, uma vez que a operação esteja em andamento, suas execuções posteriores são ignoradas.
- O teste de posição aberta no segundo AND é o que torna inofensiva uma contagem que expira com a posição zerada — o bloco de fechamento só é acionado quando há posição a fechar.
- Ambos os blocos de entrada enviam suas execuções para um Combination, de modo que o Position protection é armado por uma única entrada, qualquer que seja o lado aberto, e o preço de fechamento alimenta o mesmo bloco, dando ao take-profit e ao trailing stop um valor com que trabalhar a cada candle finalizado.
- O painel do gráfico desenha os candles, ambas as médias, o RSI, as ordens de entrada e de abandono, as ordens de proteção e cada execução, de modo que uma operação abandonada é fácil de distinguir de outra que atingiu o alvo.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
