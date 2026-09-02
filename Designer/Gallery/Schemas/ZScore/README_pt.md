# Diagrama da estratégia de reversão por z-score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O fechamento é convertido em z-score: a distância até uma média móvel medida em desvios padrão. Assim um único número descreve o quanto o mercado se esticou, qualquer que seja o preço do ativo. O diagrama se posiciona contra o exagero e devolve a operação assim que o escore volta para perto de zero.

![schema](schema.svg)

## Visão geral da estratégia

- O z-score é montado à mão a partir de SimpleMovingAverage e StandardDeviation: (Close - SMA) / StandardDeviation em um único bloco de fórmula.
- Uma fórmula espelhada devolve o mesmo escore com sinal trocado, de modo que um nível de entrada e um de saída atendem aos dois lados, sem precisar de quatro constantes.
- As entradas só ocorrem com a posição zerada, e os blocos de entrada ainda carregam a condição de abertura de posição, de modo que o diagrama nunca reforça uma operação já aberta.
- O original usa candles de um minuto e trava as operações por 500 barras após cada negócio. O histórico incluído é de cinco minutos, então o diagrama trabalha em candles de cinco minutos; a trava não é reproduzida porque o Designer não tem um contador de barras com estado, e por isso o diagrama negocia com mais frequência e segura menos tempo.

## Regras de entrada e saída

- **Entrada comprada**: O z-score está abaixo do nível de entrada negativo, ou seja, o fechamento está mais desvios padrão abaixo da média do que o configurado, e a posição está zerada. A ordem compra o volume configurado.
- **Entrada vendida**: O z-score está acima do nível de entrada, ou seja, o fechamento está mais desvios padrão acima da média do que o configurado, e a posição está zerada. A ordem vende o volume configurado.
- **Saída**: A compra é encerrada quando o z-score volta acima do nível de saída; a venda, quando ele cai abaixo desse nível negativo. Não há stop nem alvo, exatamente como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 10 | Período da média móvel a partir da qual o escore é medido. |
| StandardDeviation Length | 10 | Período do desvio padrão pelo qual a distância é dividida. |
| Entry z-score | 1.5 | Distância até a média, em desvios padrão, que abre uma operação. |
| Exit z-score | 0.5 | Distância até a média, em desvios padrão, em que a operação aberta é devolvida. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o conversor do preço de fechamento e os dois blocos de indicador, configurados para emitir apenas quando formados.
- Dois blocos de fórmula constroem o escore e o seu oposto a partir das mesmas três entradas, de modo que as comparações espelhadas dispensam constantes extras.
- Quatro blocos de comparação testam os dois escores contra os níveis de entrada e saída, e outros três comparam a posição com zero.
- Cada E lógico une uma condição de escore a uma de posição; os blocos de entrada tiram o volume de uma constante compartilhada e os de encerramento usam a condição de fechar posição, dispensando volume.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
