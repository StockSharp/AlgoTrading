# Diagrama da estratégia de scalping com cruzamento de EMA, RSI e MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um seguidor de tendência de curto prazo que não aceita um cruzamento no escuro. A EMA rápida cruzar a lenta é apenas o gatilho; antes de a ordem sair, o preço também precisa estar do lado certo de uma EMA de tendência bem mais lenta, o RSI precisa estar dentro da sua faixa de trabalho e não em um extremo, e a linha MACD precisa continuar se movendo no sentido da operação. Toda posição é entregue a um stop e a um alvo de proteção, de modo que um scalp nunca fica aberto indefinidamente.

![schema](schema.svg)

## Visão geral da estratégia

- Três médias móveis exponenciais têm funções diferentes: o par rápido e lento produz o sinal, e a longa diz qual lado do mercado é permitido.
- O bloco de cruzamento dispara apenas no instante em que a média rápida troca de lado, então uma mesma tendência não gera uma sequência de entradas.
- O RSI serve de filtro de extremos e não de sinal: um cruzamento só é aceito enquanto o índice fica entre o piso e o teto, o que afasta o diagrama de movimentos já esgotados.
- A linha MACD é comparada com o próprio valor de um candle atrás, portanto o momento precisa concordar com o cruzamento em vez de apenas existir.
- A verificação da posição faz com que uma entrada só possa abrir uma operação, nunca aumentá-la.

## Regras de entrada e saída

- **Entrada comprada**: A EMA rápida cruza acima da lenta, o candle fecha acima da EMA de tendência, o RSI está entre o piso e o teto, a linha MACD está mais alta que um candle atrás e a posição está zerada. A ordem compra o volume compartilhado a mercado.
- **Entrada vendida**: A EMA rápida cruza abaixo da lenta, o candle fecha abaixo da EMA de tendência, o RSI está entre o piso e o teto, a linha MACD está mais baixa que um candle atrás e a posição está zerada. A ordem vende o volume compartilhado a mercado.
- **Saída**: O bloco de proteção de posição encerra cada operação com um stop ou um alvo percentual medidos a partir do preço de execução. O original dimensiona os dois níveis pelo intervalo verdadeiro médio, stop a dois ATR e alvo ao dobro desse risco, mas o bloco de proteção só aceita um valor fixo, então a distância por ATR foi trocada por uma porcentagem da mesma ordem de grandeza neste instrumento; voltar à versão dinâmica exigiria recalcular os níveis no diagrama e enviar as ordens à mão. Outras duas coisas ficaram de fora: a pausa de dez barras após cada operação, que nenhum bloco consegue contar entre candles, e a virada no sinal contrário, já que aqui o stop e o alvo encerram a operação. O original trabalha em candles de trinta minutos e este diagrama roda nos candles de cinco minutos do histórico embarcado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA Length | 12 | Período da média móvel exponencial rápida que produz o cruzamento. |
| Slow EMA Length | 26 | Período da média móvel exponencial lenta contra a qual a rápida cruza. |
| Trend EMA Length | 55 | Período da média móvel exponencial de tendência que decide qual lado é permitido. |
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| RSI floor | 35 | Borda inferior da faixa do RSI; abaixo dela o cruzamento é tratado como movimento já percorrido. |
| RSI ceiling | 65 | Borda superior da faixa do RSI; acima dela o cruzamento é tratado como sobreaquecido. |
| Take profit, % | 1 | Distância do take profit em relação ao preço de execução, em porcentagem. |
| Stop loss, % | 0.5 | Distância do stop loss em relação ao preço de execução, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os cinco indicadores e um conversor que lê o preço de fechamento; o MACD usa os mesmos períodos doze e vinte e seis do par de EMA.
- O bloco de cruzamento recebe a média rápida na entrada de cima e a lenta na de baixo, e um NÃO lógico transforma essa mesma saída no cruzamento para baixo do lado vendido.
- A faixa do RSI são duas comparações contra duas constantes, compartilhadas pelas duas entradas; o teste de momento do MACD compara a linha com um bloco de valor anterior de um candle atrás.
- Cada E lógico reúne o cruzamento, o lado da tendência, as duas bordas do RSI, o teste de momento e a checagem de posição zerada, e então aciona um bloco de entrada que obtém o volume da constante compartilhada.
- Os dois blocos de entrada mandam as próprias execuções para o bloco de proteção de posição, que é quem fecha a posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
