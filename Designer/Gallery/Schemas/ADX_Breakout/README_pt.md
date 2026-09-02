# Diagrama da estratégia de rompimento do ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A maioria dos diagramas compara um indicador com um nível fixo. Este compara o índice direcional médio consigo mesmo: uma média móvel simples da linha ADX é o centro, uma banda é construída em volta dela a partir da distância atual entre as duas, e romper essa banda é lido como uma explosão repentina de força de tendência. A direção vem do candle que a produziu: se ele fecha acima da abertura, compra; em qualquer outro caso, vende.

![schema](schema.svg)

## Visão geral da estratégia

- A linha ADX do índice direcional médio é a única entrada de toda a construção; as linhas +DI e -DI não são usadas.
- Essa linha alimenta um segundo bloco de indicador, uma média móvel simples de vinte períodos, ou seja, o diagrama calcula um indicador sobre outro indicador.
- Um bloco de fórmula monta a banda como a média mais o multiplicador vezes o dobro da distância absoluta entre o ADX e sua média, exatamente como o código original calcula.
- As entradas viram uma posição aberta com uma única ordem, porque o volume é o volume compartilhado mais o que já está em carteira.

## Regras de entrada e saída

- **Entrada comprada**: A linha ADX está acima da banda, o candle fechou acima da sua abertura e a posição não está comprada. A ordem compra o volume compartilhado mais o tamanho da venda aberta, então uma ordem a mercado fecha a venda e abre a compra.
- **Entrada vendida**: A linha ADX está acima da banda, o candle fechou na abertura ou abaixo dela e a posição não está vendida. A ordem vende o volume compartilhado mais o tamanho da compra aberta.
- **Saída**: A posição é encerrada assim que a linha ADX cai abaixo da própria média móvel: a compra por uma venda em modo de fechamento e a venda por uma compra em modo de fechamento. Além disso, um bloco de proteção de posição carrega o stop loss de dois por cento do original; o take profit do original está em zero, isto é, desativado, então também não há alvo aqui. Vale saber antes de otimizar: enquanto o multiplicador ficar abaixo de 0.5, a condição da banda é algebricamente igual a «ADX acima da sua média», de modo que no valor padrão 0.1 a banda não acrescenta nada e o diagrama se lê apenas como o ADX cruzando a própria média para cima e para baixo. O multiplicador foi mantido como constante para que valores maiores se comportem como no original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ADX Length | 14 | Período de suavização do índice direcional médio. |
| Average Length | 20 | Período da média móvel simples que suaviza a linha ADX. |
| Multiplier | 0.1 | Multiplicador da largura da banda; abaixo de 0.5 a banda colapsa sobre a própria média móvel. |
| Stop Loss % | 2 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes, antes de somar a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador ADX e dois conversores que leem a abertura e o fechamento do candle.
- Um conversor extrai a linha ADX do valor do indicador complexo e a entrega tanto ao bloco de média móvel quanto às comparações.
- Um único bloco de fórmula calcula toda a banda em uma expressão, mantendo a aritmética do original em um lugar legível em vez de uma cadeia de blocos pequenos.
- Um segundo bloco de fórmula soma o módulo da posição ao volume compartilhado, e as duas saídas são acionadas diretamente pela comparação «ADX abaixo da sua média», agindo apenas quando há algo a encerrar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
