# Diagrama da estratégia de rompimento do CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Commodity Channel Index passa a maior parte do tempo entre -100 e +100, portanto sair dessa faixa é lido como o início de um movimento e não como um exagero. O diagrama compara o índice com o seu próprio valor de um candle atrás, que é o que transforma um nível em rompimento, e está sempre no mercado: cada sinal inverte a posição em vez de apenas encerrá-la.

![schema](schema.svg)

## Visão geral da estratégia

- Um bloco de indicador calcula o Commodity Channel Index e um bloco de valor anterior guarda a leitura do candle passado, de modo que o par descreve um cruzamento do nível e não somente a permanência acima dele.
- Os dois níveis são constantes comuns, então a faixa de rompimento pode ser alargada, estreitada e otimizada como qualquer outro parâmetro.
- O volume da ordem é o volume base mais o valor absoluto da posição atual, de forma que uma única ordem a mercado encerra a posição contrária e abre a nova.
- A estratégia original pula dois candles após cada sinal; esse contador não tem equivalente em blocos e foi omitido, então este diagrama pode inverter um ou dois candles antes do código de origem.
- O original trabalha em candles de uma hora; o diagrama foi reduzido para candles de cinco minutos, de acordo com o histórico de amostra incluído.

## Regras de entrada e saída

- **Entrada comprada**: O CCI fechou o candle anterior no nível superior ou abaixo dele e agora está acima, e a posição ainda não está comprada. A ordem compra o volume base mais a venda em aberto, invertendo a posição para comprada.
- **Entrada vendida**: O CCI fechou o candle anterior no nível inferior ou acima dele e agora está abaixo, e a posição ainda não está vendida. A ordem vende o volume base mais a compra em aberto, invertendo a posição para vendida.
- **Saída**: Não há saída própria: a estratégia permanece no mercado e o rompimento contrário encerra a operação atual e abre a nova. O código original também não tem stop loss nem take profit.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| CCI Length | 20 | Período de suavização do Commodity Channel Index. |
| Upper level | 100 | Nível que o índice precisa cruzar para cima em um rompimento comprado. |
| Lower level | -100 | Nível que o índice precisa cruzar para baixo em um rompimento vendido. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se a posição aberta. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o Commodity Channel Index, cuja saída vai tanto para os blocos de comparação quanto para o bloco de valor anterior.
- Dois blocos de comparação por lado testam a leitura atual e a anterior contra a mesma constante de nível, reproduzindo com exatidão a condição de rompimento do código de origem.
- Cada E lógico une a leitura atual, a leitura anterior e uma checagem de posição antes de acionar um bloco de modificação de posição.
- Um bloco de fórmula soma o volume base ao valor absoluto da posição e alimenta os dois blocos de ordem, de modo que uma ordem a mercado executa toda a inversão.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
