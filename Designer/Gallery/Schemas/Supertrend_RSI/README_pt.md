# Diagrama da estratégia Supertrend + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama seguidor de tendência com um freio de oscilador. O SuperTrend, uma banda de ATR que se arrasta atrás do preço e vira junto com ele, decide a direção, enquanto o RSI decide se o movimento ainda tem espaço: a compra só é feita enquanto o RSI está abaixo da linha média e a venda apenas enquanto está acima. A saída não é um sinal, e sim um take-profit e um stop-loss percentuais colocados sobre o negócio de entrada.

![schema](schema.svg)

## Visão geral da estratégia

- O SuperTrend é montado com um ATR de dez períodos multiplicado por três, de modo que a linha avança atrás do preço e só vira quando o fechamento a rompe.
- O RSI funciona como freio e não como sinal de reversão: a entrada é permitida enquanto o oscilador está no lado calmo do nível cinquenta, o que mantém o diagrama fora de movimentos já esticados.
- As entradas ocorrem somente a partir de posição zerada, tanto pela comparação explícita da posição com zero quanto pela condição de abertura nos blocos de ordem.
- Toda a saída é delegada a um bloco de proteção com take-profit de dois por cento e stop-loss de um por cento, exatamente o par que a estratégia original aciona.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está acima da linha SuperTrend, o RSI está abaixo da média de cinquenta e a posição está zerada. A ordem compra o volume compartilhado a mercado e o bloco de proteção arma imediatamente o take-profit e o stop-loss sobre o negócio resultante.
- **Entrada vendida**: O fechamento está abaixo da linha SuperTrend, o RSI está acima da média de cinquenta e a posição está zerada. A ordem vende o volume compartilhado a mercado e o bloco de proteção arma igualmente as duas saídas.
- **Saída**: Não há saída por sinal nem inversão: a posição é encerrada pela primeira das duas ordens protetoras a ser atingida, o take-profit de dois por cento ou o stop-loss de um por cento.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SuperTrend ATR Period | 10 | Período do ATR dentro do SuperTrend; valores maiores alargam a banda e tornam as viradas mais raras. |
| SuperTrend Multiplier | 3 | Multiplicador do ATR do SuperTrend, a distância da linha de arrasto em relação ao preço mediano. |
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| RSI Midline | 50 | Nível de RSI contra o qual o filtro de entrada é medido; o código original compara com cinquenta e não com os níveis de sobrevenda e sobrecompra que declara. |
| Take Profit, % | 2 | Distância do take-profit em relação ao preço de entrada, em porcentagem. |
| Stop Loss, % | 1 | Distância do stop-loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o SuperTrend, o RSI e um conversor que lê o preço de fechamento do mesmo candle.
- A comparação do fechamento com a saída do SuperTrend fornece o sinalizador de tendência de alta; um NÃO lógico sobre ele fornece o de baixa, por isso as duas direções nunca disparam no mesmo candle.
- Uma única constante de cinquenta atende às duas comparações de RSI, de modo que mover a linha média move os dois filtros ao mesmo tempo.
- Cada E lógico une três condições — tendência, oscilador e posição zerada — e aciona um bloco de modificação de posição que ainda carrega a condição de abertura.
- Os dois blocos de modificação entregam o próprio negócio ao bloco de proteção, que coloca as ordens de take-profit e stop-loss usando o fechamento do candle corrente como preço.
- A pausa de cem candles que o código original mantém entre operações não foi reproduzida: entre os blocos disponíveis não há contador de candles, então as entradas voltam assim que a proteção zera a posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
