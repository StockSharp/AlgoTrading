# Entradas limitadas no cruzamento de EMA com Level 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama preserva o sinal EMA(14)/EMA(50) de TwoDLimitsStrategy e torna visível a gestão das ordens. Cada candle finalizado de cinco minutos captura Level 1, coloca um limite além da melhor cotação e cancela a ordem não executada no cruzamento inverso.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados alimentam EMA rápida(14) e lenta(50); dois blocos Crossing detectam as duas direções.
- BestBidPrice e BestAskPrice chegam assincronamente de Level 1 e são retidos em cada candle antes do cálculo dos preços.
- A compra fica 0,02% abaixo do melhor bid e a venda 0,02% acima do melhor ask, dando função observável a Order cancellation.
- Cada execução inicia cooldown N values de 100 candles; até terminar, bloqueia entradas e preços da Position protection.
- Depois do cooldown, Position protection usa stop de 0,3% e alvo de 0,6%.

## Regras de entrada e saída

- **Entrada comprada**: EMA(14) cruza acima da EMA(50), Position está zerada ou vendida, existe bid positivo e o cooldown terminou. Uma compra é colocada abaixo do bid; o volume é abs(Position) mais o volume base para fechar o short e abrir long numa execução.
- **Entrada vendida**: EMA(14) cruza abaixo da EMA(50), Position está zerada ou comprada, existe ask positivo e o cooldown terminou. Uma venda é colocada acima do ask com a mesma regra de reversão líquida.
- **Saída**: O cruzamento inverso cancela o último limite oposto ainda ativo. Após a execução e 100 candles, Position protection sai no stop de 0,3% ou alvo de 0,6%; um limite oposto executado também pode inverter Position.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado para EMAs, captura das cotações, cooldown e verificação da proteção. |
| Fast EMA Length | 14 | Número de valores de cinco minutos na média exponencial rápida. |
| Slow EMA Length | 50 | Número de valores de cinco minutos na média exponencial lenta. |
| Quote Offset | 0.02% | Percentual afastado da melhor cotação: abaixo do bid na compra e acima do ask na venda. |
| Base Volume | 1 | Tamanho da nova posição após compensar a exposição oposta existente. |
| Cooldown, candles | 100 | Candles finalizados após cada execução antes de reativar entradas e proteção. |
| Stop Loss | 0.3% | Distância percentual do stop em relação à execução de entrada. |
| Take Profit | 0.6% | Distância percentual do alvo de lucro em relação à execução de entrada. |

## Detalhes do diagrama

- O C# entra a mercado; o diagrama usa deliberadamente limites de Level 1 para mostrar registro e cancelamento.
- As distâncias originais de 200/400 passos viram aproximadamente 0,3%/0,6% no preço BTCUSDT do replay, preservando 1:2.
- O código não verifica stop nem alvo nos primeiros 100 candles após execução; bloquear o preço reproduz essa ordem.
- As travas de cotação alinham Level 1 assíncrono à decisão EMA por candle. O ajuste de preço fica desligado porque pode faltar passo no replay.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
