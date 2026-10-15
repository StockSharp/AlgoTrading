# Diagrama da estratégia MACD + Stochastic com cruzamento do lado do zero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um cruzamento do MACD significa coisas diferentes conforme o lugar em que acontece. Este diagrama aceita o cruzamento de alta apenas enquanto a linha MACD ainda está abaixo de zero, que é onde nasce uma nova arrancada, e o de baixa apenas enquanto ela ainda está acima. As linhas do Stochastic confirmam a direção, a posição precisa estar zerada antes da entrada, e um stop e um alvo percentuais tiram do mercado.

![schema](schema.svg)

## Visão geral da estratégia

- O gatilho é o cruzamento da linha MACD com o sinal; o filtro de sinal confere o valor atual e o do candle anterior, de modo que uma barra que salta ao mesmo tempo sobre o zero e sobre o sinal não seja confundida com um cruzamento novo.
- O Stochastic Oscillator é a segunda opinião: a compra quer %K acima de %D e a venda quer %K abaixo.
- Só se entra a partir da posição zerada: o diagrama nunca aumenta a operação nem inverte por sinal; o stop e o alvo são a única saída.
- O diagrama expressa as distâncias do stop e do alvo como porcentagens do preço de entrada e opera durante todo o dia.
- A confirmação do Stochastic está sempre ativa e compara as duas linhas atuais. O diagrama usa candles de cinco minutos de acordo com o histórico de amostra incluído.

## Regras de entrada e saída

- **Entrada comprada**: A linha MACD cruza o sinal para cima, o valor atual e o anterior do MACD estão abaixo de zero, %K está acima de %D e a posição está zerada. A ordem compra um lote a mercado.
- **Entrada vendida**: A linha MACD cruza o sinal para baixo, o valor atual e o anterior do MACD estão acima de zero, %K está abaixo de %D e a posição está zerada. A ordem vende um lote a mercado.
- **Saída**: O bloco de proteção encerra a operação a uma porcentagem fixa do preço de entrada, no alvo ou no stop. Não há saída pelo cruzamento contrário do MACD.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MACD fast length | 12 | Período da EMA rápida dentro do MACD. |
| MACD slow length | 26 | Período da EMA lenta dentro do MACD. |
| MACD signal length | 9 | Período da EMA que suaviza o MACD até a linha de sinal. |
| Stochastic %K length | 5 | Período de cálculo da linha %K do Stochastic. |
| Stochastic %D length | 3 | Período de suavização da linha %D, a média móvel de %K. |
| Volume | 1 | Volume da ordem, em lotes. |
| Take profit, % | 1 | Distância do alvo, em porcentagem do preço de entrada. |
| Stop loss, % | 1 | Distância do stop, em porcentagem do preço de entrada. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o MACD e o Stochastic Oscillator; quatro conversores retiram os valores Macd, Signal, %K e %D dos dois valores de indicador.
- Um bloco de cruzamento transforma o par do MACD no gatilho de alta e um bloco NÃO o inverte no de baixa, enquanto um bloco de valor anterior guarda a linha MACD do candle passado para a checagem de sinal.
- Sete blocos de comparação formam os filtros: quatro para os dois testes do zero, dois para as linhas do Stochastic e um para a posição diante do zero.
- Cada E lógico une cinco condições e aciona um bloco de modificação de posição que envia ordem a mercado pela constante de volume compartilhada; os dois blocos de ordem passam seu negócio ao bloco de proteção, que ainda lê o fechamento do candle como preço atual.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
