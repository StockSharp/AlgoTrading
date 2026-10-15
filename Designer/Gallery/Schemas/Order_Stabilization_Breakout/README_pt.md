# Diagrama da estratégia de rompimento após estabilização com ordens limitadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama observa candles finalizados de cinco minutos de BTCUSDT@BNBFT em busca da transição de um corpo estabilizado abaixo da metade do ATR(14) para um corpo expandido acima desse nível. Ele coloca uma ordem limitada no fechamento do sinal, concede à ordem três candles posteriores para terminar e dobra a quantidade base de uma reversão executada.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos fornecem os preços Open e Close, enquanto valores formados de ATR(14) medem a escala atual de volatilidade.
- Body é calculado como `abs(Close - Open)`, e seu limite de estabilização é `ATR * Stabilization Factor`. O fator padrão é `0.5`.
- O primeiro par Body/ATR formado inicializa os valores anteriores armazenados sem criar sinal. Cada par formado posterior compara os corpos anterior e atual com seus respectivos limites.
- Uma configuração exige `Previous Body < Previous ATR * 0.5` e `Current Body > Current ATR * 0.5`. Igualdade em qualquer limite não atende à condição.
- Uma trava compartilhada de ordem pendente permite somente uma ordem limitada ativa. Portas de vigência separadas de compra e venda impedem cada lado de reutilizar seu contador de três candles antes de ele terminar.

## Regras de entrada e saída

- **Entrada comprada**: Quando um candle de expansão válido é de alta (`Close > Open`), o estado com sinal está zerado ou vendido, não há ordem pendente e a porta de vigência da compra está pronta, envia uma compra limitada no Close atual.
- **Entrada vendida**: Quando um candle de expansão válido é de baixa (`Close < Open`), o estado com sinal está zerado ou comprado, não há ordem pendente e a porta de vigência da venda está pronta, envia uma venda limitada no Close atual.
- **Quantidade da ordem**: A quantidade é `Base Volume * (1 + abs(state))`. Uma entrada com estado zerado usa uma unidade base; uma reversão aceita de `-1` ou `1` usa duas unidades base.
- **Saída**: Não há proteção baseada em preço. A exposição muda apenas quando uma limitada oposta é executada; uma limitada não executada recebe um pedido de cancelamento direcionado após três candles finalizados estritamente posteriores.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Security | BTCUSDT@BNBFT | Ativo usado pela assinatura de candles finalizados de cinco minutos. Strategy Security deve receber o mesmo valor, pois ordens, cancelamentos e execuções usam Strategy Security e Strategy Portfolio. |
| Candle Series | 00:05:00 | Candles finalizados de cinco minutos usados para ATR, cálculo do corpo, sinais, contagem de vigência e gráfico. |
| ATR Length | 14 | Período de média do Average True Range. As decisões começam somente quando o ATR está formado. |
| Stabilization Factor | 0.5 | Multiplicador aplicado separadamente aos valores anterior e atual de ATR para formar seus limites de corpo. |
| Lifetime N | 3 | Número de candles finalizados estritamente posteriores permitido antes de pedir o cancelamento de uma limitada não executada. |
| Base Volume | 1 | Quantidade da entrada com estado zerado; a fórmula de ação a dobra em uma reversão. |

## Detalhes do diagrama

- A [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Security configura somente a assinatura de [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) finalizados. Cada candle concluído fornece Open, Close e o candle completo passado ao ATR; os blocos de ordem e negociação usam Strategy Security e Strategy Portfolio, portanto Strategy Security deve coincidir com Security.
- O bloco [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) emite apenas valores formados de ATR(14). Blocos de fórmula e trava mantêm Body atual, limite atual, Body anterior e limite anterior alinhados dentro de uma decisão por candle.
- Uma trava de inicialização suprime a primeira decisão formada e armazena seu par. As decisões posteriores avaliam as duas relações estritas com os limites antes de avançar as travas dos valores anteriores.
- O candle atual alcança ambos os contadores de vigência antes da execução do ramo de sinal. Iniciar um contador após essa entrada faz sua liberação ocorrer no terceiro candle finalizado posterior, portanto o candle de sinal nunca conta na própria vigência.
- Uma configuração aceita fecha a porta do contador de seu lado e ativa a trava pendente compartilhada antes de disparar [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html). A trava pendente é limpa somente quando a ordem informa um estado final; o contador do lado continua indisponível até sua liberação de três candles mesmo quando a ordem é executada antes.
- Cada bloco de registro armazena sua referência Order para cancelamento direcionado. A liberação da vigência envia a referência armazenada correspondente ao cancelamento e reabre somente a porta do contador daquele lado.
- Eventos MyTrade definem o estado com sinal a partir de execuções reais: uma venda executada define `-1`, uma compra executada define `1`, e o registro começa em `0` para zerado. O gráfico recebe candles, ATR, Body, limite de estabilização, ordens enviadas e execuções da estratégia.

## Uso

Importe o arquivo `.json` no Designer, defina Strategy Security como BTCUSDT@BNBFT, execute-o sobre histórico de cinco minutos e revise execuções e cancelamentos das limitadas antes de ajustar fator, vigência ou quantidade para outro ambiente de negociação.
