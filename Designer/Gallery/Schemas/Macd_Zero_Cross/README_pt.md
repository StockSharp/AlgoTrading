# Diagrama da estratégia de cruzamento da linha zero do MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O MACD é a distância entre uma média móvel exponencial rápida e uma lenta, portanto o próprio sinal da linha MACD já diz qual das médias está por cima. Este diagrama ignora a linha de sinal e opera exatamente no momento em que a linha MACD muda de sinal: de negativa para zero ou positiva, compra; de zero ou positiva para negativa, vende.

![schema](schema.svg)

## Visão geral da estratégia

- O MACD é calculado com período rápido 8, lento 17 e de sinal 9; apenas a linha MACD participa das decisões, a linha de sinal é calculada mas nunca lida.
- Um bloco de valor anterior guarda a linha MACD do candle precedente, de modo que a troca de sinal é reconhecida como um cruzamento real e não como um estado que apenas perdura.
- A posição atual entra em cada condição, então um sinal no sentido já mantido é descartado em vez de aumentar a posição.

## Regras de entrada e saída

- **Entrada comprada**: A linha MACD estava abaixo de zero no candle anterior e está em zero ou acima no atual, e a posição não está comprada. A ordem compra o volume fixo: abre uma compra a partir do zero ou encerra uma venda existente.
- **Entrada vendida**: A linha MACD estava em zero ou acima no candle anterior e está abaixo no atual, e a posição não está vendida. A ordem vende o volume fixo: abre uma venda a partir do zero ou encerra uma compra existente.
- **Saída**: Não há bloco de saída próprio nem stop de proteção: todas as ordens usam o mesmo volume, então o cruzamento contrário do zero devolve a posição ao zero em vez de invertê-la, e a próxima posição só é aberta no cruzamento seguinte.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA length | 8 | Período da média móvel exponencial rápida dentro do MACD. |
| Slow EMA length | 17 | Período da média móvel exponencial lenta dentro do MACD. |
| Signal EMA length | 9 | Período de suavização da linha de sinal do MACD; não influencia as decisões. |
| Volume | 1 | Volume da ordem, em lotes; o mesmo valor serve para abrir e para fechar. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco do indicador MACD, e um conversor extrai a linha MACD do valor do indicador complexo.
- Um bloco de valor anterior desloca essa linha um candle para trás, e quatro blocos de comparação testam o valor anterior e o atual contra uma constante zero compartilhada.
- A mesma constante zero é comparada com o bloco de posição, o que produz os dois filtros Posição <= 0 e Posição >= 0.
- Cada E lógico une três condições - valor anterior, valor atual e posição - e aciona um bloco de modificação de posição que envia uma ordem a mercado com a constante de volume compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
