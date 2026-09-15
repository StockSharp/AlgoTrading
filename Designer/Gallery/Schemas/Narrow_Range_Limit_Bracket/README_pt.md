# Diagrama da estratégia Narrow Range Limit Bracket
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um mercado que para de se mover é um mercado se preparando para se mover. Este diagrama mede a altura de cada candle, encontra o momento em que essa altura encolhe até o menor valor de vários candles e cerca esse candle com dois níveis: sua máxima acima e sua mínima abaixo. O nível além do qual o preço fechar primeiro decide a direção da operação.

![schema](schema.svg)

## Visão geral da estratégia

- Candles de 15 minutos finalizados alimentam três conversores que leem a máxima, a mínima e o fechamento de cada candle.
- Uma fórmula subtrai a mínima da máxima, de modo que cada candle se reduz a um único número: sua amplitude.
- O indicador Lowest trabalha sobre esse número, e não sobre o preço, e informa a menor amplitude dos últimos seis candles; uma fórmula a multiplica pelo Squeeze Factor para obter a largura em que um candle precisa caber para ser considerado estreito.
- Um segundo ramo reconstrói a mesma medição um candle atrás: Previous value entrega o candle anterior, dois conversores e uma fórmula dão sua amplitude, e outro Previous value pega a leitura que o indicador Lowest tinha naquele momento.
- Uma condição lógica junta três respostas: este candle é estreito, o candle anterior não era e a posição está zerada. Isso é o squeeze.
- No squeeze, a máxima e a mínima desse candle são gravadas em duas variáveis, que seguem mantendo esses dois preços candle após candle: o bracket. Uma terceira variável o arma.
- A partir do candle seguinte, duas comparações acompanham o fechamento em relação aos dois níveis, e uma condição lógica libera uma entrada a mercado quando o fechamento fica além de um deles, o bracket está armado e a posição está zerada.
- Dois painéis do gráfico desenham o resultado: o preço com os níveis do bracket, as ordens e as execuções em um; a amplitude do candle contra seu limite de squeeze no outro.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento de um candle está acima do nível superior do bracket, o bracket está armado e a posição está zerada. Position modify compra o Order Volume a mercado.
- **Entrada vendida**: O fechamento de um candle está abaixo do nível inferior do bracket, sob as mesmas condições de bracket armado e posição zerada. Position modify vende o Order Volume a mercado.
- **Saída**: O diagrama não tem sinal de saída próprio. As duas entradas são unidas pelo Combination em um único fluxo de execuções, que alimenta o Position protection: ele assume a posição e a fecha a 550 unidades de preço de lucro ou 550 unidades de preço de prejuízo, medidas a partir do preço de execução. O mesmo fluxo de execuções desarma o bracket, de modo que um nível já negociado não pode ser negociado outra vez.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:15:00 | Time frame dos candles com que todo o diagrama trabalha. |
| Period | 6 | Número de candles sobre os quais o indicador Lowest mede a menor amplitude. |
| Squeeze Factor | 1.08 | Quanto o candle atual ainda pode ser mais largo que a menor amplitude recente e mesmo assim contar como estreito. |
| Expansion Factor | 1.05 | Quanto o candle anterior precisava ser mais largo que sua própria menor amplitude recente para o squeeze contar como novo. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Take Profit | 550 | Distância do take-profit a partir do preço de execução, em unidades de preço. |
| Stop Loss | 550 | Distância do stop-loss a partir do preço de execução, em unidades de preço. |

## Detalhes do diagrama

- O squeeze é a comparação de um candle com seu próprio histórico recente, por isso não precisa de uma ideia fixa do que é um candle estreito: o Squeeze Factor apenas diz quão perto da menor amplitude recente a amplitude atual tem de chegar.
- O Expansion Factor protege o outro lado da mesma ideia. Sem ele, uma sequência de candles igualmente calmos continuaria sinalizando de novo; exigir que o candle anterior tenha sido mais largo que seu próprio limite faz o sinal marcar o momento em que a volatilidade encolhe, e não todo o trecho calmo.
- O indicador Lowest recebe um número em vez de um candle, e é isso que permite que um indicador comum de preço funcione como medidor de volatilidade.
- O bracket é armado pelo squeeze e desarmado por uma execução de entrada, e seus níveis permanecem no lugar até que o próximo squeeze os substitua. Um bracket, portanto, vive até ser negociado ou até surgir um novo squeeze, em vez de expirar por contagem de barras.
- O squeeze também exige posição zerada, de modo que os níveis nunca são reescritos enquanto uma operação está aberta, e a condição de posição nos dois blocos de entrada recusa uma segunda ordem sobre uma posição aberta.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
