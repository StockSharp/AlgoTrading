# Diagrama da estratégia de rompimento na janela da sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um rompimento só vale a pena enquanto houver com quem negociar. Este diagrama mede a faixa de vinte candles, abre posição quando um candle finalizado fecha fora dela e se recusa a agir se esse candle não pertencer a uma janela fixa do dia. Tudo o que vem depois da entrada fica a cargo de um stop e de um take percentuais.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles de cinco minutos comanda todo o diagrama, e apenas candles finalizados são publicados, de modo que cada decisão é tomada sobre uma barra que já não pode mudar.
- Highest e Lowest, ambos com vinte candles de comprimento e ambos somente formados, carregam a borda superior e a borda inferior da faixa recente.
- Previous value desloca cada borda um candle para trás. É esse deslocamento que transforma uma faixa em nível de rompimento: o candle que está sendo avaliado não pode fazer parte do limite que ele precisa superar.
- Um conversor lê o fechamento do candle atual, e duas comparações o confrontam com os dois limites deslocados.
- Working time responde a uma pergunta por candle - este candle pertence à janela de negociação? - e devolve um simples verdadeiro ou falso no mesmo compasso das comparações.
- Duas condições lógicas unem rompimento e janela, de modo que a quebra de um nível fora da janela não produz absolutamente nada e o diagrama fica ocioso pelo resto do dia.
- As duas entradas são ordens a mercado de volume fixo e ambas trazem a condição Open position, de modo que uma ordem só é enviada a partir de posição zerada, nunca para aumentar ou inverter uma posição existente.
- Position protection recebe as execuções de entrada e o fechamento do candle e assume a operação a partir daí, encerrando-a a um percentual fixo de lucro ou de prejuízo.

## Regras de entrada e saída

- **Entrada comprada**: Um candle finalizado fecha acima da máxima de vinte candles tomada um candle atrás, e esse candle pertence à janela de negociação. Position modify compra o volume da ordem a mercado; a condição Open position só deixa a ordem passar enquanto a posição estiver zerada.
- **Entrada vendida**: Um candle finalizado fecha abaixo da mínima de vinte candles tomada um candle atrás, sob a mesma condição de janela. Position modify vende o volume da ordem a mercado, novamente apenas a partir de posição zerada.
- **Saída**: Não há sinal de saída no diagrama. Aberta a posição, Position protection assume o comando: precifica a operação a partir da execução de entrada, acompanha o fechamento do candle e encerra a 1.5% de lucro ou 0.5% de prejuízo - um alvo três vezes maior que o risco. A janela governa apenas as entradas, portanto uma posição aberta bem no fim dela sobrevive ao encerramento da janela até que um dos seus dois limites seja atingido. Um rompimento contrário nesse meio-tempo é ignorado, porque a condição Open position bloqueia qualquer entrada que não parta de posição zerada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Time frame da série de candles; apenas candles finalizados chegam ao diagrama. |
| Breakout High Length | 20 | Número de candles sobre os quais o limite superior é medido, antes de o deslocamento de um candle ser aplicado. |
| Breakout Low Length | 20 | Número de candles sobre os quais o limite inferior é medido. Mantenha igual ao comprimento superior para que as duas bordas descrevam a mesma faixa. |
| Session From | 12:00:00 | Início da janela de negociação como horário do dia. Um candle aberto antes dele não pode disparar uma entrada. |
| Session Until | 21:00:00 | Fim da janela de negociação. Um candle aberto depois dele não pode disparar uma entrada; uma posição já aberta não é afetada. |
| Order Volume | 1 | Quantidade fixa enviada pelas duas entradas a mercado. |
| Take Profit, % | 1.5 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 0.5 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- Os dois indicadores de faixa são somente formados e somente finais, de modo que nenhuma leitura parcial pode mover um limite, e os vinte primeiros candles não produzem sinal algum.
- O deslocamento de um candle é aplicado à saída do indicador, e não ao preço. Um valor de indicador levado um passo para trás é exatamente o limite tal como estava antes de o candle atual existir, e é contra ele que um rompimento precisa ser medido.
- Working time lê a marca de tempo trazida pelo valor que recebe. Um candle carrega o horário de sua abertura, então um candle conta como dentro da janela quando foi aberto dentro dela.
- As execuções das duas entradas são unidas em um único fluxo antes de chegarem ao Position protection, de modo que um único bloco de proteção cobre igualmente posições compradas e vendidas.
- Position protection recebe o fechamento do candle como preço, de modo que o stop e o take são medidos contra os mesmos preços de candles finalizados sobre os quais a decisão de entrada foi tomada, e são verificados uma vez por candle.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
