# Diagrama da estratégia ATR Step Streak
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Duas médias móveis definem a direção, mas nada é comprado no momento em que elas trocam de lugar. O diagrama espera um número fixo de candles encerrados depois desse momento e só então pergunta se a operação ainda vale a pena. A espera é feita por um bloco N values, armado pela comparação das duas médias e que se manifesta quando os candles que ele foi mandado contar já se passaram. Uma segunda condição mede quanto espaço resta até a borda da faixa recente, e essa medida é feita em volatilidade, não em preço: o fechamento precisa estar a pelo menos um passo de ATR da máxima mais alta antes de uma compra, e ao mesmo passo acima da mínima mais baixa antes de uma venda.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de quinze minutos encerrados alimentam um conversor que extrai o preço de fechamento e cinco indicadores: uma média móvel simples rápida e uma lenta, um ATR, e a máxima mais alta e a mínima mais baixa do canal recente.
- Dois blocos Comparison comparam as médias entre si e reportam a cada candle: um é verdadeiro enquanto a média rápida está acima da lenta, o outro enquanto ela está abaixo.
- Cada comparação arma o seu próprio bloco N values. O bloco recebe o fluxo de candles na entrada, conta os candles encerrados que se seguem ao sinal de armação, libera um único pulso quando a contagem se esgota e se arma de novo na próxima comparação verdadeira, de modo que o lado de entrada do diagrama funciona num relógio mais lento do que os dados de mercado.
- Uma Formula multiplica o ATR pelo multiplicador do passo, e mais duas Formulas transformam esse passo em um par de níveis de guarda: a máxima mais alta menos o passo, e a mínima mais baixa mais o passo.
- Outras duas comparações perguntam se o fechamento ainda está abaixo do nível de guarda superior, ou ainda acima do inferior — isto é, se o preço já avançou tanto para dentro do canal que não sobrou espaço para a operação.
- Um bloco Position é comparado com uma variável zero, para que o lado de entrada saiba se a conta está zerada.
- Cada lado reúne quatro condições em um Logical condition configurado como And: o pulso do N values, a comparação das médias verificada de novo naquele instante, o espaço até a borda do canal e a posição zerada. Só quando as quatro chegam juntas é que o portão se manifesta.
- O Modify position abre a operação a mercado com o volume tomado de uma variável, mais dois blocos Modify position a encerram na comparação oposta, o Position protection é armado por cada execução de entrada, e o painel de gráfico desenha os candles, as duas médias, os dois extremos do canal, todas as ordens e todas as execuções.

## Regras de entrada e saída

- **Entrada comprada**: A média rápida está acima da lenta, o bloco N values desse lado libera o seu pulso neste candle, o fechamento está a pelo menos um passo de ATR abaixo da máxima mais alta do canal, e a posição está zerada. As quatro condições se encontram no And da compra, e o Modify position compra o volume da ordem a mercado. O bloco está configurado apenas para abrir, de modo que nada é comprado enquanto já houver posição aberta de qualquer lado.
- **Entrada vendida**: A imagem espelhada: a média rápida está abaixo da lenta, o bloco N values de baixa libera o seu pulso, o fechamento está a pelo menos um passo de ATR acima da mínima mais baixa do canal, e a posição está zerada. O And da venda passa o sinal a um bloco Modify position que vende o volume da ordem a mercado, também apenas abrindo.
- **Saída**: Há duas saídas. A troca de lugar das médias é a primeira: a comparação que fica verdadeira depois da troca aciona um bloco de fechamento para o lado que está em carteira, e esse bloco envia a posição inteira a mercado. O Position protection é a segunda — armado por cada execução de entrada, ele realiza o lucro em 2% e limita a perda em 1%, medidos contra o preço de fechamento dos candles encerrados que é alimentado na sua entrada de preço. Como os dois blocos de entrada apenas abrem, a comparação que encerra uma operação nunca abre a oposta; a próxima entrada tem de esperar o próximo pulso que encontre a conta zerada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:15:00 | Time frame dos candles com que todo o diagrama trabalha. Toda contagem do diagrama — as médias, o ATR, o canal, o período de espera — é medida nesses candles. |
| Fast SMA Length | 20 | Período da média rápida. Encurte-o e as duas médias trocam de lugar com mais frequência, o que arma o bloco de espera mais vezes e produz mais entradas. |
| Slow SMA Length | 60 | Período da média lenta. A diferença entre este e o período rápido decide quanto tempo uma tendência precisa durar para sequer ser reconhecida. |
| Bull Streak Bars | 3 | Quantos candles encerrados o bloco N values do lado comprado conta entre o momento em que as médias se alinham e o pulso que ele libera. Um faz o diagrama entrar no candle seguinte ao cruzamento; um valor grande atrasa a entrada para bem dentro do movimento e, como o bloco se rearma depois de cada pulso, também espaça mais as entradas. |
| Bear Streak Bars | 3 | A mesma espera no lado vendido. Mantenha-a igual à do lado comprado, a menos que as duas direções devam ser confirmadas em prazos diferentes. |
| ATR Length | 14 | Janela do ATR que mede a volatilidade. Ela define a unidade em que a distância até a borda do canal é expressa. |
| Step Multiplier | 2 | Quantos ATRs de espaço o preço precisa ter até a borda do canal. Aumente-o e as entradas só são feitas bem longe do extremo, o que é mais raro; reduza-o na direção de zero e a condição praticamente desaparece, deixando sozinho o sinal de tendência atrasado. |
| Channel High Length | 20 | Sobre quantos candles a máxima mais alta é tomada. É o teto sob o qual uma entrada comprada tem de permanecer, pela distância do passo. |
| Channel Low Length | 20 | Sobre quantos candles a mínima mais baixa é tomada. É o piso acima do qual uma entrada vendida tem de permanecer, pela distância do passo. Mantenha-o igual à janela da máxima, a menos que um canal assimétrico seja o que você procura. |
| Order Volume | 1 | Volume enviado pelos dois blocos de entrada. Os blocos de fechamento não levam volume: eles enviam o que a posição tiver. |
| Take Profit, % | 2 | Alvo de lucro do bloco de proteção, como percentual do preço de execução. |
| Stop Loss, % | 1 | Limite de perda do bloco de proteção, como percentual do preço de execução. Junto com o alvo, ele decide quantas operações terminam na proteção em vez de na troca de lugar das médias. |

## Detalhes do diagrama

- O bloco N values é um atraso, não um contador de barras consecutivas: ele é armado pela primeira comparação verdadeira que vê, conta os candles encerrados que se seguem e se manifesta uma única vez. Ele não verifica se a condição se manteve durante toda a janela, e é por isso que a mesma comparação é perguntada de novo dentro do And no momento em que o pulso chega — uma tendência que se desfez durante a espera não passa nessa segunda pergunta e nenhuma ordem é enviada.
- Um Logical condition limpa as suas entradas assim que se manifesta, de modo que o portão de entrada só pode ser avaliado nos candles que trazem um pulso. Entre dois pulsos as comparações continuam se atualizando, mas nada chega aos blocos de entrada, e é isso que impede que um diagrama cujas condições ficam verdadeiras por horas opere a cada candle.
- Os níveis de guarda são traçados com o mesmo ATR que mede a volatilidade, de modo que a distância que o preço tem de manter da borda do canal cresce quando o mercado se move mais rápido e encolhe quando ele se acalma. Os dois indicadores do canal incluem o candle que está sendo avaliado, então o nível acompanha os extremos em vez de ficar atrasado em relação a eles.
- As constantes do diagrama — o zero, o volume da ordem e o multiplicador do passo — são todas disparadas pelo fluxo de candles, de modo que os seus valores chegam no mesmo tick que os valores dos indicadores com os quais são comparadas e multiplicadas.
- Os blocos de fechamento estão ligados diretamente às comparações das médias, portanto são disparados em todo candle em que as médias mantêm essa ordem. Quem filtra é a sua condição de fechamento: quando não há posição naquele lado o bloco não faz nada, e nenhuma ordem sai do diagrama.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
