# Diagrama da estratégia do harami de alta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um harami é um candle que cabe inteiro dentro do anterior, sinal de que o lado que acabara de empurrar o mercado ficou sem fôlego. O código original mede essa contenção pelas extremidades e não pelos corpos, portanto o que se reconhece aqui é um inside bar que ainda muda de cor: o candle anterior foi para um lado e o pequeno candle dentro dele vai para o outro. Essa virada é assumida a partir do zero e entregue a uma média móvel simples.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de padrão de candles carregam padrões próprios escritos exatamente como o código original os verifica: o candle anterior tem uma cor, o atual tem a outra, e sua máxima e sua mínima ficam dentro da faixa anterior.
- A média móvel simples do preço de fechamento não filtra a entrada de forma alguma; ela é apenas o juiz que decide quando a operação acabou.
- As entradas só são permitidas com a posição exatamente zerada, e é isso que torna o harami uma tentativa de reversão em vez de um modo de aumentar uma operação em curso.
- As saídas são blocos de modificação de posição em modo de fechamento, de modo que nunca abrem nada por acidente.

## Regras de entrada e saída

- **Entrada comprada**: O bloco do padrão de alta informa um candle de baixa seguido por um candle de alta menor cuja máxima fica abaixo da máxima anterior e cuja mínima fica acima da mínima anterior, e a posição está zerada. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O bloco do padrão de baixa informa um candle de alta seguido por um candle de baixa menor contido do mesmo jeito, e a posição está zerada. A ordem vende um lote e abre uma venda.
- **Saída**: Uma compra é encerrada assim que um candle fecha abaixo da média móvel e uma venda assim que um candle fecha acima dela, ambas por blocos de modificação de posição em modo de fechamento, exatamente como no original. O original ainda para de operar por quinhentos candles depois de cada ordem; nenhum bloco guarda um contador de barras entre candles, então essa pausa foi removida e o diagrama simplesmente opera cada padrão que encontra enquanto está zerado. O original trabalha em candles de um minuto e o histórico embarcado é de cinco minutos, por isso o diagrama roda em candles de cinco minutos.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da média móvel simples que encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois blocos de padrão, a média móvel e um conversor que lê o preço de fechamento.
- Dois blocos de comparação colocam o fechamento de um lado ou do outro da média móvel; esses mesmos dois sinais acionam os dois blocos de fechamento.
- Um bloco de comparação testa a posição contra uma constante zero, e a sua saída é compartilhada pelas duas condições de entrada.
- Cada E lógico une um padrão à verificação de posição zerada e aciona um bloco de modificação de posição que obtém o volume da constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
