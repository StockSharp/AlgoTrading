# Diagrama da estratégia de rompimento com Choppiness Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Choppiness Index não diz para onde o mercado vai, apenas se ele está indo a algum lugar. O diagrama o usa como chave: enquanto o índice está baixo o mercado tem tendência e a posição é aberta do lado em que o fechamento ficou em relação a uma média móvel simples; quando o índice volta à zona lateral, a posição é encerrada seja qual for o resultado.

![schema](schema.svg)

## Visão geral da estratégia

- O Choppiness Index é calculado sobre catorze candles finalizados e é lido como percentual: valores baixos indicam mercado direcional, valores altos indicam congestão.
- A média móvel simples de vinte períodos fornece apenas a direção; ela não filtra nada por conta própria, porque a permissão de operar já foi dada pelo teste de regime.
- A entrada só ocorre a partir do zero, então um trecho de tendência gera uma operação e não uma pilha crescente delas.
- Não há stop nem alvo: o mesmo índice que abriu a operação é o que a encerra.

## Regras de entrada e saída

- **Entrada comprada**: O Choppiness Index está abaixo do limiar de tendência, o candle fechou acima da média móvel simples e a posição está zerada. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O Choppiness Index está abaixo do limiar de tendência, o candle fechou abaixo da média móvel simples e a posição está zerada. A ordem vende um lote e abre uma venda.
- **Saída**: Assim que o Choppiness Index sobe acima do limiar de congestão, a posição aberta é encerrada: a compra por uma venda em modo de fechamento e a venda por uma compra em modo de fechamento. O código original também não traz stop loss nem take profit. Duas coisas divergem dele de propósito. Seus limiares são 99 e 99.5, o que deixaria o filtro de entrada permanentemente aberto e a condição de saída permanentemente inatingível; por isso o diagrama usa os valores canônicos 38.2 e 61.8 da documentação do indicador, que são também os descritos no próprio README da estratégia. A pausa de quinhentas barras entre operações também foi omitida, porque um contador desses não tem equivalente fiel em blocos.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da média móvel simples que dá a direção da entrada. |
| Choppiness Length | 14 | Período de suavização do Choppiness Index. |
| Trending Threshold | 38.2 | Valor do índice abaixo do qual a entrada é permitida. |
| Choppy Threshold | 61.8 | Valor do índice acima do qual o mercado é considerado lateral e a posição é encerrada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha; o original usa candles de um minuto e este diagrama os de cinco minutos do histórico incluído. |

## Detalhes do diagrama

- O bloco de candles alimenta o Choppiness Index, a média móvel e um conversor que extrai o preço de fechamento do candle.
- Duas comparações transformam o índice em dois sinais de regime — tendência abaixo de um limiar, congestão acima do outro — e outras duas comparam o fechamento com a média.
- O bloco de posição é comparado três vezes com uma constante zero: dá a proteção de posição zerada para as entradas e as de compra e de venda para as saídas.
- Quatro E lógicos alimentam quatro blocos de modificação de posição: dois abrem posição e tiram o volume da constante compartilhada, dois apenas encerram o que já existe.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
