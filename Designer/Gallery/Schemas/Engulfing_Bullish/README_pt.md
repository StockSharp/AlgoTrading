# Diagrama da estratégia de engolfo de alta com filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um candle de engolfo indica que o lado que dominava a barra anterior acabou de ser atropelado. Sozinho isso acontece com frequência demais, por isso uma média móvel simples decide onde o sinal é aceito: o engolfo de alta só é comprado abaixo da média e o de baixa só é vendido acima dela. Essa mesma média é o alvo em que a operação é encerrada.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de indicador de padrões de candle carregam os padrões prontos Bullish Engulfing e Bearish Engulfing, de modo que a figura é reconhecida sem escrever fórmula.
- Uma média móvel simples do preço de fechamento divide o gráfico em uma metade barata e outra cara.
- O padrão só é comprado na metade barata e só é vendido na cara, o que transforma o diagrama em um exemplo de reversão à média, e não de rompimento.
- A verificação de posição garante que um padrão só seja executado quando não há posição aberta.

## Regras de entrada e saída

- **Entrada comprada**: O bloco de padrão informa um engolfo de alta, o candle fechou abaixo da média móvel e não há posição. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O bloco de padrão informa um engolfo de baixa, o candle fechou acima da média móvel e não há posição. A ordem vende um lote e abre uma venda.
- **Saída**: A compra é encerrada quando um candle fecha acima da média móvel e a venda quando fecha abaixo, ambas por blocos de modificação de posição em modo de fechamento. A estratégia original sai do mesmo lado da média em que entrou e sustenta a operação com uma pausa de várias centenas de barras; aqui não existe bloco contador de barras, então a saída é o retorno à média, a regra mais próxima que continua operando com sentido.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período da média móvel simples que filtra os padrões e encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta quatro ramos: os dois indicadores de padrão, a média móvel e um conversor que lê o preço de fechamento.
- Dois blocos de comparação colocam o fechamento de um lado ou de outro da média; esses mesmos dois sinais servem como filtro de entrada e como gatilho de saída.
- O bloco de posição é comparado com uma constante zero e o resultado protege as duas entradas.
- Cada E lógico une um padrão, um filtro e a verificação de posição, e aciona um bloco de modificação de posição; as duas ordens de entrada tiram o volume de uma constante compartilhada e os dois blocos de fechamento não precisam dele.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
