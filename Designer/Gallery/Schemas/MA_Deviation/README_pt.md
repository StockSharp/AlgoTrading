# Diagrama da estratégia de desvio da média móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A média móvel simples é tomada como preço justo, e todo o sinal é a distância do fechamento até ela, medida em percentual. Quando o preço se afasta demais da média, o diagrama entra contra o movimento e devolve a operação assim que o preço volta a tocar a média.

![schema](schema.svg)

## Visão geral da estratégia

- O desvio é calculado literalmente, em um único bloco de fórmula: (Close - SMA) / SMA * 100.
- Um só limiar serve aos dois lados: o desvio é comparado com esse número em positivo e em negativo, de modo que compra e venda ficam simétricas.
- A entrada só ocorre com posição zerada, e os dois blocos de entrada ainda carregam a condição Abrir posição, então nunca há preço médio.
- O original trabalha em candles de um minuto, com limiar de 2% e uma pausa de 500 candles após cada operação. O histórico incluído é de cinco minutos, por isso o diagrama roda em candles de cinco minutos com limiar de 1%, cerca de dois desvios padrão dessa série; a pausa não foi reproduzida porque o Designer não tem contador de bloqueio, e por isso o diagrama opera com mais frequência que o original.

## Regras de entrada e saída

- **Entrada comprada**: O desvio está abaixo do limiar negativo, ou seja, o fechamento está mais do que o percentual configurado abaixo da média, e a posição está zerada. A ordem compra o volume configurado.
- **Entrada vendida**: O desvio está acima do limiar positivo, ou seja, o fechamento está mais do que o percentual configurado acima da média, e a posição está zerada. A ordem vende o volume configurado.
- **Saída**: A compra é encerrada quando o fechamento retorna à média ou acima dela; a venda, quando o fechamento retorna à média ou abaixo dela. Não há stop loss nem take profit, como na estratégia original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da média móvel simples. |
| Deviation, % | 1 | Distância da média, em percentual, que abre uma operação. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta tanto o conversor que lê o preço de fechamento quanto o bloco de indicador com a média móvel.
- Um bloco de fórmula transforma esse par em desvio percentual; uma segunda fórmula mínima inverte o sinal da constante de limiar para que um único parâmetro cubra os dois lados.
- Dois blocos de comparação testam o desvio contra os limiares e outros dois comparam o fechamento com a média para as saídas.
- O bloco de posição é comparado com zero três vezes, gerando os indicadores zerado, comprado e vendido que os E lógicos unem às condições de preço.
- As entradas vão para blocos de modificação de posição com a condição Abrir posição e uma constante de volume compartilhada; as saídas vão para blocos com a condição Fechar posição, que dispensam volume.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
