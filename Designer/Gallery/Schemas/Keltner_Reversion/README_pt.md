# Diagrama da estratégia de reversão ao canal de Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um canal de Keltner é uma média móvel com um envelope de volatilidade: a largura vem do Average True Range, de modo que as bandas respiram com o mercado em vez de ficarem a uma distância fixa. Este diagrama trata um fechamento fora do canal como exagero, entra no sentido contrário e devolve a operação na linha média.

![schema](schema.svg)

## Visão geral da estratégia

- O canal é montado à mão em vez de usar o indicador KeltnerChannels pronto, porque aquele bloco prende a média e o ATR a um único comprimento, enquanto o original usa 20 para a EMA e 14 para o ATR.
- Dois blocos de fórmula constroem as bandas literalmente: EMA mais e menos o ATR vezes o multiplicador, com o multiplicador exposto para alargar ou estreitar o canal sem mexer no diagrama.
- A linha média é toda a regra de saída: a operação é devolvida assim que o preço volta para o outro lado da EMA, portanto o alvo caminha junto com a média.
- O original trabalha em candles de um minuto e trava as operações por 500 barras após cada negócio, o que na prática também segura a posição. O histórico incluído é de cinco minutos, então o diagrama usa candles de cinco minutos; a trava não é reproduzida porque o Designer não tem contador de barras com estado, e por isso o diagrama negocia mais vezes e segura menos tempo.

## Regras de entrada e saída

- **Entrada comprada**: O fechamento está abaixo da banda inferior, ou seja, mais de um ATR vezes o multiplicador abaixo da EMA, e a posição está zerada. A ordem compra o volume configurado.
- **Entrada vendida**: O fechamento está acima da banda superior, ou seja, mais de um ATR vezes o multiplicador acima da EMA, e a posição está zerada. A ordem vende o volume configurado.
- **Saída**: A compra é encerrada quando o fechamento volta acima da EMA e a venda quando volta abaixo dela. O original declara um multiplicador de stop que nunca utiliza, então o diagrama também não tem stop nem alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| EMA Length | 20 | Período da média móvel exponencial que forma a linha média. |
| ATR Length | 14 | Período do Average True Range que define a largura do canal. |
| ATR multiplier | 2 | Quantos ATR separam as bandas da linha média. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o conversor do preço de fechamento e os dois blocos de indicador; o ATR precisa do candle inteiro e por isso é ligado direto à fonte de candles.
- Cada banda é um bloco de fórmula com três entradas: a EMA, o ATR e a constante compartilhada do multiplicador.
- Quatro blocos de comparação testam o fechamento contra as duas bandas e contra a linha média, e outros três comparam a posição com zero.
- Cada E lógico une uma condição de preço a uma de posição; os blocos de entrada carregam a condição de abrir posição e uma constante de volume compartilhada, e os de encerramento a condição de fechar posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
