# Diagrama da estratégia Account Rules Guard
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia cruzamentos de EMA(120)/EMA(450) em candles de um minuto finalizados e envolve essa entrada comum em um supervisor no nível da conta. P&L change, uma fórmula, duas comparações, um OR lógico, uma trava Flag e um flag armazenado vigiam juntos o resultado financeiro combinado da execução. No instante em que esse resultado atinge o limite de perda ou a meta de lucro, o supervisor fecha a posição, registra o ocorrido no log e bloqueia qualquer nova entrada até o fim da execução.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de um minuto finalizados alimentam dois blocos Indicator, uma EMA rápida e uma EMA lenta, e dois blocos Crossing leem esse par nos dois sentidos.
- P&L change informa o dinheiro realizado e o não realizado na mesma atualização, e um bloco Formula soma os dois em um único resultado combinado, recalculado a cada atualização de P&L.
- Dois blocos Comparison testam o resultado combinado contra o nível Max Loss e o nível Profit Target, e um bloco Logical condition com o operador OR transforma qualquer uma das duas respostas em um único sinal de regra acionada.
- Flag trava esse sinal na primeira vez que ele é verdadeiro. Sua entrada de reset é deixada propositalmente desconectada, de modo que o supervisor funciona como uma chave de sentido único pelo restante da execução.
- Uma Variable do tipo flag armazena o estado da trava e o reemite a cada candle, que é o que um AND lógico precisa para funcionar; um bloco Logical condition com o operador NOT converte o estado armazenado na permissão lida pelos portões de entrada.
- Cada portão de entrada é um AND lógico de três coisas: um cruzamento no seu sentido, a permissão do supervisor e uma verificação de posição montada com Current position e um bloco Comparison contra zero.
- Position modify abre um Volume a mercado com a condição de abertura de posição, de modo que a entrada só é feita a partir de posição zerada; o cruzamento oposto alimenta um segundo Position modify que fecha o que está aberto e deixa o diagrama zerado, em vez de invertê-lo.
- Quando a regra é acionada, um terceiro Position modify zera a posição, uma Variable captura o resultado naquele instante, String formatter o formata e Notification o escreve no log junto com uma segunda linha que descreve a permissão de negociação da plataforma.

## Regras de entrada e saída

- **Entrada comprada**: Um candle finalizado em que a EMA rápida cruza acima da EMA lenta, com o supervisor não acionado e a posição não comprada, compra um Volume a mercado. A condição de abertura de posição significa que a entrada só é feita a partir de posição zerada: o mesmo sinal chegando enquanto já existe posição aberta é recusado, e não somado a ela.
- **Entrada vendida**: Um candle finalizado em que a EMA rápida cruza abaixo da EMA lenta, com o supervisor não acionado e a posição não vendida, vende um Volume a mercado. Assim como no lado comprado, a condição de abertura de posição admite a entrada apenas a partir de posição zerada.
- **Saída**: A saída comum é o cruzamento oposto: o bloco Position modify de fechamento zera o que estiver aberto, de modo que o diagrama volta a ficar zerado e espera um novo cruzamento em vez de inverter a posição. A saída de emergência é o supervisor: assim que o resultado combinado realizado e não realizado atinge o nível Max Loss ou o nível Profit Target, a posição é fechada a mercado, a trava é ativada, o valor e a permissão da plataforma são escritos no log, e nenhuma nova entrada é admitida pelo restante da execução.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:01:00 | Time frame da série de candles. Apenas candles finalizados movimentam as médias móveis, o estado reemitido da trava, o pulso de volume e a leitura da permissão. |
| Fast EMA Length | 120 | Período da média móvel exponencial rápida. |
| Slow EMA Length | 450 | Período da média móvel exponencial lenta. |
| Max Loss | -5000 | Resultado combinado realizado e não realizado, na moeda da conta, no qual ou abaixo do qual o supervisor é acionado. É escrito como um número negativo e é deliberadamente amplo: um limite ajustado perto demais interrompe o diagrama antes que ele tenha negociado o suficiente para mostrar qualquer coisa. |
| Profit Target | 10000 | Resultado combinado realizado e não realizado no qual ou acima do qual o supervisor é acionado. Alcançá-lo encerra a execução da mesma forma que uma perda: posição fechada, trava ativada, nenhuma nova entrada. |
| Volume | 1 | Quantidade fixa usada por ambos os blocos de entrada. Os dois blocos de fechamento tomam a sua quantidade da posição aberta e ignoram este valor. |

## Detalhes do diagrama

- [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) emite o dinheiro realizado e o não realizado em conjunto, e a [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `r + u` os soma no valor que ambos os blocos [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) avaliam. O bloco permanece silencioso até que a conta realmente se mova, de modo que o supervisor não pode ser acionado antes do primeiro negócio executado, e as duas Variables de limite são disparadas pelo próprio resultado combinado, para que os dois lados de cada comparação sempre cheguem na mesma atualização.
- A permissão é armazenada, não transmitida em fluxo. [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) emite apenas no instante em que é ativado, o que um AND lógico não consegue aproveitar, porque o AND espera um valor em cada entrada e as limpa assim que dispara. O estado da trava, portanto, vive em uma [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) do tipo flag cujo padrão é false e cuja entrada Trigger é o fluxo de candles: a cada candle ela reemite o estado atual, e um bloco [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) com o operador NOT o converte na permissão de entrada.
- O operador OR sobre o sinal de regra acionada é uma escolha deliberada: ao contrário do AND, ele não espera um valor em cada entrada, de modo que qualquer um dos limites sozinho pode levantá-lo. Ele também emite uma resposta falsa a cada atualização sem evento, o que não custa nada adiante: Flag ignora um gatilho falso, as Variables de captura o ignoram e [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) se recusa a agir sobre ele, de modo que nenhuma ordem é enviada por uma resposta negativa.
- Os blocos de fechamento usam a condição de fechamento de posição e não precisam de nenhuma entrada de volume: a quantidade é tomada da posição que está aberta. Os blocos de entrada mantêm o seu próprio Volume, e [Current position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) comparado contra zero dá a cada portão as mesmas verificações de não comprado e não vendido que a regra de entrada declara.
- [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) lê a permissão da própria plataforma a cada candle e é mantido fora dos portões de entrada de propósito: em histórico gravado ele responde "não permitido" durante toda a execução, de modo que um portão construído sobre ele nunca abriria e o diagrama não negociaria nada. Sua resposta é capturada em uma Variable e formatada pelo [String formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) na segunda linha da [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html), que é onde ela pertence: explica o estado da plataforma no momento em que a regra foi acionada, em vez de silenciar o diagrama. Notification está configurado para o tipo log, o único tipo entregue enquanto o histórico está sendo reproduzido.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
