# Diagrama da estratégia do expert advisor KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma adaptação do expert advisor KDJ do MetaTrader. A linha J é reconstruída aqui como a diferença entre as linhas %K e %D do oscilador estocástico, e é essa diferença que escolhe o lado: compra quando ela fica positiva ou quando %K continua subindo com a diferença já positiva, e vende nas condições espelhadas. Duas coisas foram adaptadas ao histórico empacotado: os candles de quatro horas do original passaram a ser de uma hora, para que um mês de dados ainda ofereça barras suficientes, e o stop e o alvo em pips viraram distâncias percentuais que valem para qualquer instrumento.

![schema](schema.svg)

## Visão geral da estratégia

- O oscilador estocástico com %K de 30 barras e %D de 6 faz as vezes do KDJ, e a diferença K - D cumpre o papel da linha J.
- Há duas formas de entrar: a diferença cruzando o zero, ou a linha %K andando na direção que o sinal da diferença já aponta.
- A posição só é aberta a partir do zero, portanto a estratégia nunca piramida nem inverte; quem encerra o negócio é o bloco de proteção.

## Regras de entrada e saída

- **Entrada comprada**: K - D é positiva e, ou era negativa no candle anterior, o que faz deste candle o cruzamento do zero, ou %K está acima do valor do candle anterior. A posição precisa estar zerada; compra-se um lote a mercado.
- **Entrada vendida**: K - D é negativa e, ou era positiva no candle anterior, o que faz deste candle o cruzamento do zero, ou %K está abaixo do valor do candle anterior. A posição precisa estar zerada; vende-se um lote a mercado.
- **Saída**: Não existe sinal de saída algum, exatamente como no original: o bloco de proteção encerra a operação com ordens a mercado em um alvo de 2% ou um stop de 1%, o equivalente percentual das distâncias de 450 e 250 pips do código.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| %K Length (KDJ period) | 30 | Comprimento da linha %K, o período KDJ do advisor original. |
| %D Smoothing | 6 | Comprimento de suavização da linha %D. |
| Take profit, % | 2 | Distância do alvo, em percentual do preço de entrada. |
| Stop loss, % | 1 | Distância do stop, em percentual do preço de entrada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 01:00:00 | Tempo gráfico dos candles de todo o diagrama; o original usava quatro horas. |

## Detalhes do diagrama

- Dois blocos conversores separam o estocástico nas linhas %K e %D, e um bloco de fórmula subtrai uma da outra.
- Blocos de valor anterior guardam K - D e %K um candle atrás, e é assim que o cruzamento do zero e a inclinação são reconhecidos sem um bloco de cruzamento.
- Quatro blocos E lógico montam as duas vias de entrada de cada direção e já carregam o sinalizador de posição zerada; um OU junta o par em um único disparo por lado.
- Os dois blocos de entrada passam seus próprios negócios ao bloco de proteção, de modo que cada execução recebe imediatamente stop e alvo.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
