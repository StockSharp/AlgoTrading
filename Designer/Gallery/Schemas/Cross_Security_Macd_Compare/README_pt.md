# Diagrama da estratégia de comparação de MACD entre instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama negocia um instrumento e decide com base em dois. Um MACD é medido no instrumento negociado e em um instrumento de referência, e o que se compara não são os preços deles, mas o momentum: quando o lado negociado é o mais fraco dos dois na leitura rápida e ainda o mais forte na leitura lenta, o diagrama trata isso como uma defasagem que ainda não se fechou e o compra. A imagem espelhada vende.

![schema](schema.svg)

## Visão geral da estratégia

- O bloco Index constrói um instrumento sintético a partir de uma expressão e o entrega a um segundo bloco de candles, que é a forma como um segundo instrumento entra em um diagrama.
- As duas pernas são assinadas como séries de candles do mesmo time frame: uma no instrumento da estratégia, outra no instrumento que o Index produziu.
- O Sync mantém uma linha por perna e libera as duas juntas, de modo que uma leitura do instrumento negociado e uma leitura do instrumento de referência sempre descrevam o mesmo candle, e não a série que por acaso tenha chegado.
- Cada perna recebe seu próprio MACD, e conversores extraem dele três números: a linha MACD, a linha de sinal e o preço de fechamento do candle correspondente.
- Duas fórmulas por perna transformam esses números em percentuais do preço da própria perna — o histograma, MACD menos sinal, e a própria linha de sinal. Comparar os valores brutos não faria sentido quando os dois instrumentos têm preços em ordens de grandeza diferentes.
- Quatro variáveis leem os quatro percentuais no candle negociado, de modo que tanto a comparação quanto a ordem que a segue são temporizadas pelo instrumento que está sendo negociado, e não pela perna que fechou por último.
- As comparações colocam as pernas lado a lado — histograma contra histograma, sinal contra sinal — e quatro condições lógicas acrescentam o estado da posição, um portão por ação: abrir comprado, abrir vendido, fechar comprado, fechar vendido.
- As entradas são ordens a mercado de volume fixo, feitas apenas a partir de posição zerada; a leitura oposta fecha, e o Position protection carrega um take-profit e um stop-loss em percentual do preço de entrada.

## Regras de entrada e saída

- **Entrada comprada**: Em um candle negociado, o histograma do instrumento negociado está abaixo do histograma de referência enquanto sua linha de sinal está acima da linha de sinal de referência, e a posição está zerada. A leitura rápida diz que o lado negociado está atrasado, a leitura lenta diz que ele ainda está à frente, e o diagrama lê o par como uma defasagem a ser recuperada. O Position modify compra o volume da ordem a mercado.
- **Entrada vendida**: Em um candle negociado, o histograma do instrumento negociado está acima do histograma de referência enquanto sua linha de sinal está abaixo da linha de sinal de referência, e a posição está zerada. O Position modify vende o volume da ordem a mercado.
- **Saída**: Há duas saídas e qualquer uma delas pode vir primeiro. A leitura espelhada fecha a posição: uma compra sai quando o histograma passa à frente do de referência e a linha de sinal fica atrás dela, uma venda sai no par oposto. Cada uma é um Position modify configurado para fechar, de modo que o volume vem da própria posição e nada é invertido dentro de uma única ordem — o diagrama volta a ficar zerado e espera um novo sinal. Independentemente disso, o Position protection acompanha cada execução de entrada e fecha com 1.6% de lucro ou 0.8% de perda em relação ao preço de entrada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Reference Security | TONUSDT@BNBFT * 1 | Expressão a partir da qual o bloco Index constrói o instrumento de referência. Ela nomeia um instrumento e o multiplica por um, o que deixa o preço intacto e mantém o parâmetro como uma expressão aritmética. |
| Traded Candles | 00:15:00 | Time frame da série de candles negociada, e o compasso sobre o qual cada ordem é construída. |
| Reference Candles | 00:15:00 | Time frame da série de candles de referência. Precisa coincidir com o da série negociada, caso contrário as duas pernas nunca caem no mesmo candle. |
| Sync Interval | 00:15:00 | Duração do bloco de tempo em que o Sync agrupa as duas pernas; a mesma duração dos candles. |
| Traded MACD Fast | 12 | Comprimento da média móvel rápida do MACD construído sobre o instrumento negociado. |
| Traded MACD Slow | 26 | Comprimento da média móvel lenta do mesmo MACD. A distância entre ele e o comprimento rápido decide quanto tempo um movimento precisa durar antes que o histograma reaja. |
| Traded MACD Signal | 9 | Comprimento da linha de sinal do mesmo MACD; é a linha contra a qual o histograma é medido. |
| Reference MACD Fast | 12 | Comprimento da média móvel rápida do MACD construído sobre o instrumento de referência. Cada perna carrega seus próprios três comprimentos, de modo que as duas podem ser ajustadas separadamente, mas uma comparação dos dois histogramas só significa algo enquanto ambos estiverem configurados iguais. |
| Reference MACD Slow | 26 | Comprimento da média móvel lenta do MACD de referência. Mantenha-o igual ao do MACD negociado, a menos que os dois instrumentos devam ser medidos em horizontes diferentes. |
| Reference MACD Signal | 9 | Comprimento da linha de sinal do MACD de referência. Mantenha-o igual ao do MACD negociado pelo mesmo motivo. |
| Order Volume | 1 | Tamanho da ordem, em lotes. |
| Take Profit, % | 1.6 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 0.8 | Distância do stop-loss, em percentual do preço de entrada. |

## Detalhes do diagrama

- Os dois blocos de candles aceitam apenas candles finalizados. Uma atualização de um candle em formação carrega o horário de abertura do candle, e uma ordem construída a partir dela seria mais antiga que o momento em que é enviada.
- O Sync libera um conjunto sob o horário mais antigo dos valores contidos nele, e é por isso que nada vindo do Sync chega diretamente a uma ordem: as quatro variáveis releem os valores no candle negociado, e é no compasso desse candle que as ordens são construídas. O custo é que uma comparação usa o último par completo de leituras, um candle atrás do candle sobre o qual ela age.
- Toda variável que guarda uma constante — o zero e o volume da ordem — é disparada pelo candle negociado. Uma variável sem gatilho mantém seu valor e nunca o emite, e uma condição à espera dela nunca se completaria.
- Os dois blocos MACD emitem apenas valores formados e finais, de modo que a comparação começa assim que cada perna tem um indicador completo por trás de si e não pode se mover dentro de um candle.
- As duas pontas de cada linha do Sync estão ligadas. Um valor enviado para dentro e nunca retirado deixa o bloco à espera dele, e a estratégia não inicia.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
