# Diagrama da estratégia Candle Streak Profit Lock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Quatro candles que fecham todos da mesma forma são uma sequência, e este diagrama opera na direção dessa sequência. Entrar é a metade fácil; a metade interessante é sair, porque o diagrama não espera um nível de preço para decidir isso. Ele observa o dinheiro da posição aberta e, na primeira vez que esse valor atinge um montante definido, a posição é fechada e o lucro é realizado. Uma trava garante que esse comando seja enviado uma única vez, e não a cada atualização do resultado.

![schema](schema.svg)

## Visão geral da estratégia

- Os candles finalizados de quatro horas são lidos uma vez e reaproveitados: o candle atual vai direto para um conversor de fechamento e um de abertura, e três blocos Previous value devolvem os candles de uma, duas e três barras atrás, cada um com seu próprio conversor de fechamento e de abertura.
- Oito comparações transformam esses quatro pares na cor de cada barra: fechamento acima da abertura é um candle de alta, fechamento abaixo da abertura é um de baixa, e uma barra que fecha exatamente onde abriu não é nem um nem outro, pois falha nos dois testes e não pode estender nenhuma das sequências.
- Um AND lógico reúne as quatro respostas de alta e um segundo reúne as quatro de baixa; cada um emite verdadeiro apenas quando as quatro barras da janela concordam, que é justamente o que define uma sequência.
- O bloco de posição comparado com zero informa se o diagrama está zerado, comprado ou vendido, e cada porta a jusante é um AND lógico entre uma sequência e um estado de posição.
- As duas entradas são ordens a mercado de volume fixo e carregam a condição de posição aberta, de modo que uma sequência que continua enquanto já existe uma posição aberta não pode empilhar uma segunda ordem sobre a primeira.
- As execuções de entrada são unidas em uma única linha e entregues ao Position protection, que mantém um take-profit e um stop móvel como percentual do preço de execução e os mantém cotados enquanto a posição existir.
- O bloco Strategy P&L emite o resultado aberto da posição em andamento em seu próprio ritmo, e uma comparação confronta esse valor com o Profit Lock; a resposta vai para um Flag, que deixa passar o primeiro verdadeiro e depois permanece acionado, de modo que exatamente um comando de fechamento é enviado.
- Uma sequência que se forma contra uma posição aberta a fecha a mercado, e o Flag é rearmado por qualquer uma das portas de entrada, de modo que cada nova posição começa com sua trava novamente pronta.

## Regras de entrada e saída

- **Entrada comprada**: Quatro candles finalizados seguidos fecham acima de suas próprias aberturas enquanto a posição está zerada: o Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: Quatro candles finalizados seguidos fecham abaixo de suas próprias aberturas enquanto a posição está zerada: o Position modify vende o volume da ordem a mercado.
- **Saída**: São três saídas, e a posição sai por aquela que chegar primeiro. A trava de lucro a fecha assim que o resultado aberto atinge o montante definido; como o Flag já deixou esse sinal passar uma vez, as atualizações posteriores do mesmo resultado não enviam nada. O Position protection a fecha pelo take-profit ou pelo stop móvel, com o stop acompanhando o preço assim que a operação fica no lucro. Uma sequência na direção oposta também a fecha a mercado, e essa ordem de fechamento é tudo o que acontece naquela barra: as portas de entrada exigem posição zerada, então uma reversão é aberta pelo próximo sinal de sequência que encontrar o diagrama zerado, e não pelo mesmo sinal que o zerou.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Type | 04:00:00 | Time frame da série de candles sobre a qual a sequência é contada; as quatro barras de uma sequência são quatro candles desse comprimento. |
| Order Volume | 1 | Tamanho da ordem, em lotes, enviado por cada entrada; ele também escala o resultado aberto com o qual o Profit Lock é comparado. |
| Take Profit | 1% | Distância do take-profit em relação ao preço de execução, em percentual. |
| Stop Loss | 1% | Distância do stop-loss em relação ao preço de execução, em percentual; o stop é móvel, portanto acompanha o preço assim que a operação avança a favor e nunca retrocede. |
| Profit Lock | 200 | Resultado aberto, no dinheiro do portfólio, no qual a posição é fechada e o lucro é realizado. |

## Detalhes do diagrama

- O comprimento da sequência está embutido no diagrama, e não definido como um número: quatro barras significam três blocos Previous value e quatro entradas em cada porta de sequência. Uma sequência de cinco são três blocos a mais e uma entrada a mais por porta; uma sequência de três é um bloco a menos.
- Cada bloco Previous value é tipado como candle e lê a série de candles diretamente, e o fechamento e a abertura são retirados do que ele devolve. Pegar primeiro um preço e só depois pedir o seu valor anterior é a ordem inversa, e é justamente a que deixa a comparação sem um operando.
- O Profit Lock é um valor no dinheiro do portfólio, e não uma distância em preço, portanto depende tanto do volume da ordem quanto do instrumento. Dobrar o volume reduz pela metade o movimento necessário para alcançá-lo; trocar para um instrumento de outra escala de preços muda tudo.
- A trava e o take-profit são duas respostas para a mesma pergunta, e vence a mais apertada. Ajuste a trava para disparar antes do take-profit e o take passa a ser o teto, alcançado apenas quando uma barra salta além da trava; ajuste-a larga e o take-profit é a saída habitual, com a trava como rede de segurança atrás dele.
- Tudo o que o diagrama envia são ordens a mercado, e a série de candles é assinada somente como barras finalizadas. Um sinal construído a partir de uma barra ainda em formação carrega o horário de abertura dessa barra, e uma ordem marcada com um horário já no passado é recusada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
