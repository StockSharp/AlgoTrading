# Diagrama da estratégia de reversão no preenchimento de gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama mede o salto entre o fechamento de um candle e a abertura do seguinte e depois espera que esse candle feche no sentido contrário. Um gap de baixa seguido de candle de alta é comprado, um gap de alta seguido de candle de baixa é vendido, e a SimpleMovingAverage decide quando a operação termina.

![schema](schema.svg)

## Visão geral da estratégia

- O gap é medido em porcentagem do fechamento anterior, assim o mesmo limiar mantém o significado em qualquer nível de preço.
- O gap sozinho não é sinal: o candle que abriu longe do fechamento anterior precisa fechar de volta na direção dele, e é esse o corpo de reversão que dá nome à estratégia.
- A SimpleMovingAverage é a única linha de saída e serve aos dois lados; não há stop nem alvo, exatamente como no código original.
- O diagrama roda em candles de um minuto, como a estratégia de origem, portanto o gap aqui é a pequena descontinuidade entre dois minutos vizinhos, não um gap de abertura diária.

## Regras de entrada e saída

- **Entrada comprada**: A distância entre a abertura e o fechamento anterior é de pelo menos Min Gap %, a abertura fica abaixo do fechamento anterior, o candle fecha acima da própria abertura e não há posição. A ordem compra um lote a mercado.
- **Entrada vendida**: A distância entre a abertura e o fechamento anterior é de pelo menos Min Gap %, a abertura fica acima do fechamento anterior, o candle fecha abaixo da própria abertura e não há posição. A ordem vende um lote a mercado.
- **Saída**: A compra é encerrada no primeiro candle que fecha abaixo da SimpleMovingAverage e a venda no primeiro que fecha acima; os dois blocos de encerramento calculam o volume a partir da posição aberta.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Min Gap % | 0.02 | Distância mínima entre o fechamento anterior e a nova abertura, em porcentagem do fechamento anterior. |
| SMA Length | 20 | Período de suavização da SimpleMovingAverage que encerra a posição. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:01:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Dois blocos conversores leem a abertura e o fechamento do candle, e um bloco de valor anterior guarda o fechamento do candle precedente.
- O bloco de fórmula converte a distância entre a abertura e o fechamento anterior em porcentagem, e uma comparação a confronta com a constante de limiar.
- Outras quatro comparações informam o lado do gap e o lado do corpo; cada E lógico une a condição de gap, a de corpo e a checagem de posição zerada antes do bloco de ordem.
- O par de saída compara o fechamento com a média móvel e aciona dois blocos de encerramento. A pausa de 500 barras entre operações existente no código não tem equivalente entre os blocos e foi omitida, por isso o diagrama negocia com mais frequência.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
