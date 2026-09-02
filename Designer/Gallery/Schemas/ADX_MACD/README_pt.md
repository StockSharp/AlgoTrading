# Diagrama da estratégia ADX + MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois indicadores clássicos dividem o trabalho: o MACD em relação à sua linha de sinal mostra para que lado o mercado pende e o ADX diz se o movimento é forte o bastante para valer a pena. As entradas exigem os dois, enquanto a saída ouve apenas o MACD, de modo que a posição é encerrada assim que o impulso vira, mesmo com a tendência ainda medindo forte.

![schema](schema.svg)

## Visão geral da estratégia

- A linha ADX é retirada do valor composto do índice direcional médio e comparada com um único limiar de força.
- A direção vem do nível da linha MACD em relação à sua linha de sinal, e não do instante do cruzamento, então uma nova posição pode ser aberta a qualquer momento enquanto o MACD permanecer de um lado.
- O filtro de força protege apenas as entradas: a saída dispara somente pela passagem do MACD para o outro lado, e o diagrama não tem stop nem alvo.

## Regras de entrada e saída

- **Entrada comprada**: O ADX está acima do limiar, a linha MACD está acima da linha de sinal e a posição está zerada. O bloco de modificação compra a mercado o volume compartilhado.
- **Entrada vendida**: O ADX está acima do limiar, a linha MACD está abaixo da linha de sinal e a posição está zerada. O bloco de modificação vende a mercado o volume compartilhado.
- **Saída**: A compra é encerrada quando a linha MACD cai abaixo da linha de sinal e a venda quando sobe acima dela; na saída o filtro de ADX não é consultado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ADX Length | 14 | Período do índice direcional médio, que define tanto o índice direcional quanto a sua suavização. |
| ADX Threshold | 25 | Nível de força que a linha ADX precisa superar para permitir uma entrada. |
| Fast EMA length | 12 | Período da EMA rápida dentro do MACD. |
| Slow EMA length | 26 | Período da EMA lenta dentro do MACD. |
| Signal EMA length | 9 | Período da EMA de sinal calculada sobre a linha MACD. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores; conversores extraem a linha ADX do índice direcional médio e as linhas MACD e de sinal do indicador MACD.
- Três comparações produzem as condições de mercado — força da tendência, MACD acima da linha de sinal e MACD abaixo dela — e outras três comparam a posição com zero.
- Os blocos E de entrada juntam força, direção e posição zerada; os de saída juntam direção com uma posição aberta do lado oposto.
- A pausa de 100 candles que a estratégia em C# mantém entre operações não pode ser montada com blocos do Designer, por isso este diagrama entra e sai com mais frequência.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
