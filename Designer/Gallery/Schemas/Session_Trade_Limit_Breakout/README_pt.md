# Diagrama da estratégia Session Trade Limit Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um rompimento só vale a pena quando o mercado esteve calmo, apenas dentro do horário em que o diagrama tem permissão para operar e apenas uma vez, até que o direito de operar seja devolvido. O bloco Flag é o que garante essa última parte: ele deixa passar o primeiro rompimento qualificado como um único pulso e depois permanece fechado, de modo que uma sequência de velas fortes produz uma única entrada em vez de uma ordem por barra.

![schema](schema.svg)

## Visão geral da estratégia

- Tudo o que vem depois trabalha sobre velas de trinta minutos já fechadas, portanto nenhuma decisão é tomada sobre uma barra ainda em formação.
- Um bloco Highest sobre vinte velas marca o topo da faixa recente, e um bloco Previous value toma esse nível de uma vela atrás — o nível fica, assim, fixado antes da vela que precisa superá-lo.
- Um Average Directional Index de período quatorze é reduzido à sua própria linha por um conversor, e uma comparação restringe as entradas a um mercado que ainda está calmo: a linha do ADX abaixo do seu limite.
- O Working time responde, vela a vela, se o momento cai dentro da janela de negociação, e um NOT lógico da mesma resposta é o que marca o fim da sessão.
- Um AND lógico reúne quatro respostas em um único portão: o fechamento está acima do nível de rompimento, a linha do ADX está abaixo do limite, o momento está dentro da janela e a posição está zerada.
- O bloco Flag transforma esse portão em um bilhete. A primeira resposta verdadeira é repassada como um único pulso e dispara uma compra a mercado por meio do Position modify com a condição de abrir posição; toda resposta posterior é engolida enquanto o bilhete está gasto.
- Duas coisas devolvem o bilhete: um bloco de atraso que conta quinze velas fechadas depois da execução da entrada, e a primeira vela que se imprime fora da janela de negociação.
- A saída é construída sobre as mesmas duas leituras que permitiram a entrada — um limite de ADX esticado e um nível ligeiramente abaixo do nível de rompimento — unidas por um OR lógico em um único gatilho de fechamento, com o Position protection funcionando por baixo como take-profit e stop-loss fixos.

## Regras de entrada e saída

- **Entrada comprada**: A vela fecha acima da máxima mais alta das vinte velas anteriores, a linha do ADX está abaixo do limite de mercado calmo, a vela pertence à janela de negociação e a posição está zerada. As quatro condições chegam na mesma vela, o AND lógico as transforma em uma única resposta verdadeira, e o Flag repassa a primeira dessas respostas ao Position modify, que compra o volume da ordem a mercado.
- **Entrada vendida**: Não há entradas vendidas. O diagrama é deliberadamente unilateral: o rompimento que ele procura é de alta, e uma queda de volta através do nível de devolução é lida como motivo para sair de uma posição comprada, e não como motivo para vender.
- **Saída**: Duas leituras encerram a operação, e o OR lógico significa que a que vier primeiro já basta. A primeira é um mercado que deixou de estar calmo: uma fórmula multiplica o limite de mercado calmo pelo multiplicador de tendência e uma comparação verifica se a linha do ADX alcançou esse nível esticado. A segunda é um rompimento que se devolveu: uma segunda fórmula reduz o nível de rompimento pelo fator de devolução e uma comparação verifica se o fechamento caiu abaixo dele. Qualquer uma delas dispara um Position modify configurado para fechar a posição. Por baixo das duas, o Position protection acompanha a execução da entrada e encerra a operação com dois por cento de lucro ou um por cento de prejuízo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:30:00 | Time frame em que trabalham todos os blocos do diagrama; apenas velas fechadas são processadas. |
| Breakout Length | 20 | Número de velas que o bloco Highest olha para trás para construir o nível de rompimento. |
| ADX Length | 14 | Período do Average Directional Index cuja linha é usada como filtro de mercado calmo. |
| Calm Market Limit | 25 | Valor abaixo do qual a linha do ADX precisa se manter para que um rompimento conte como saído de um mercado tranquilo. |
| Session From | 12:00:00 | Início da janela de negociação; uma vela anterior a ele não pode abrir posição. |
| Session Until | 21:00:00 | Fim da janela de negociação; a primeira vela depois dele devolve o bilhete de entrada. |
| Cooldown Candles | 15 | Velas fechadas que precisam passar após a execução de uma entrada antes que o bilhete de entrada seja devolvido. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Trend Multiplier | 1.5 | Multiplicador aplicado ao limite de mercado calmo para obter o nível de ADX que encerra a operação. |
| Give-Back Factor | 0.98 | Fração do nível de rompimento abaixo da qual o fechamento precisa cair para a saída por devolução. |
| Take Profit, % | 2 | Distância do take-profit, em porcentagem do preço de entrada. |
| Stop Loss, % | 1 | Distância do stop-loss, em porcentagem do preço de entrada. |

## Detalhes do diagrama

- A verificação de posição zerada dentro do portão de entrada tem peso real. Sem ela, um rompimento que chegasse com a operação já aberta gastaria o bilhete em uma ordem que a condição de abrir posição recusaria, e o diagrama então ficaria parado durante todo o período de espera por nada.
- O Flag emite apenas no momento em que é acionado, portanto sua saída vai direto para o gatilho da ordem e nunca para o AND lógico — é um pulso, não um nível, e por isso todas as condições são reunidas antes dele, e não ao seu lado.
- O atraso que mede o período de espera é armado pela execução da entrada e alimentado pela série de velas, de modo que os quinze valores que ele conta são quinze barras fechadas.
- O bloco Highest lê a máxima de cada vela, portanto o nível rompido é a máxima mais alta das últimas vinte barras, não o fechamento mais alto, e o rompimento é correspondentemente mais rigoroso.
- O take-profit e o stop-loss ficam ao lado das duas comparações de saída, e não no lugar delas: são a proteção de reserva para uma operação que fica à deriva sem que nenhuma das leituras dispare.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
