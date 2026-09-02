# Diagrama da estratégia de repique com média móvel e estocástico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois blocos decidem juntos: a SimpleMovingAverage diz de que lado do mercado o diagrama pode operar e a StochasticK espera um movimento contrário a esse lado antes de enviar a ordem. A posição é devolvida assim que o preço fecha do outro lado da mesma média.

![schema](schema.svg)

## Visão geral da estratégia

- A direção vem do fechamento em relação à SimpleMovingAverage: acima dela só se consideram compras, abaixo só vendas.
- A entrada é contrária ao movimento imediato: a linha %K precisa estar na zona de sobrevenda para comprar e na de sobrecompra para vender, ou seja, o diagrama compra recuos dentro da alta e vende repiques dentro da baixa.
- StochasticK é exatamente o %K que a estratégia original calculava manualmente: 100 * (Close - menor Low) / (maior High - menor Low) nas últimas N velas.
- A mesma média móvel também é a linha de saída, e não há stop nem alvo em nenhum ponto do diagrama.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está acima da SimpleMovingAverage, a StochasticK abaixo do nível de sobrevenda e não há posição. A ordem compra um lote a mercado.
- **Entrada vendida**: O fechamento está abaixo da SimpleMovingAverage, a StochasticK acima do nível de sobrecompra e não há posição. A ordem vende um lote a mercado.
- **Saída**: A compra é encerrada no primeiro candle que fecha abaixo da média e a venda no primeiro que fecha acima; os dois blocos de encerramento tiram o volume da posição aberta.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da SimpleMovingAverage que filtra a tendência e encerra a posição. |
| %K Length | 14 | Número de candles que a linha %K observa para trás. |
| %K Oversold | 20 | Nível abaixo do qual %K é considerado sobrevendido e a compra é liberada. |
| %K Overbought | 80 | Nível acima do qual %K é considerado sobrecomprado e a venda é liberada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta três ramos: o conversor que lê o fechamento, a SimpleMovingAverage e o indicador StochasticK.
- Duas comparações colocam o fechamento diante da média, outras duas colocam %K diante das constantes de limiar e uma compara a posição com zero.
- Cada E lógico une a condição de tendência, a do estocástico e a checagem de posição zerada, e então aciona um bloco de modificação que só abre a partir do zero.
- As comparações de tendência são reaproveitadas na saída: o mesmo sinal que libera a venda encerra a compra, o que mantém o diagrama enxuto. O contador que parava a estratégia original por 100 candles após cada operação não tem bloco correspondente e foi omitido.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
