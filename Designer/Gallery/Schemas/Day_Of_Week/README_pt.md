# Diagrama da estratégia do efeito dia da semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O calendário decide a direção e a média móvel decide o momento. No começo da semana o diagrama pode comprar, no fim da semana pode vender, e nos dois casos ele espera o preço de fechamento ficar do lado correspondente de uma média móvel simples antes de agir. O dia da semana é lido direto do candle, portanto nada precisa ser guardado de um candle para o outro.

![schema](schema.svg)

## Visão geral da estratégia

- Um conversor extrai o dia da semana do horário de abertura do candle como número, em que domingo é zero e sábado é seis.
- Cada janela do calendário é formada por duas comparações: segunda a terça para o lado comprado e quinta a sexta para o vendido, com os limites expostos como parâmetros para mover ou alargar a janela.
- Uma média móvel simples do preço de fechamento confirma a direção; só o calendário nunca abre uma operação.
- A posição atual participa das duas entradas, então o diagrama nunca aumenta uma operação que já mantém.

## Regras de entrada e saída

- **Entrada comprada**: O candle pertence à janela do começo da semana, seu fechamento está acima da média móvel simples e a posição está zerada. A ordem compra o volume compartilhado a mercado.
- **Entrada vendida**: O candle pertence à janela do fim da semana, seu fechamento está abaixo da média móvel simples e a posição está zerada. A ordem vende o volume compartilhado a mercado.
- **Saída**: Um fechamento de volta abaixo da média encerra uma compra e um fechamento de volta acima encerra uma venda, ambos por blocos de modificação de posição em modo de fechamento. Como um bloco de fechamento nada faz com a posição já zerada, isso reproduz o teste de cruzamento do original sem blocos extras. O original tem dois contadores que o diagrama não consegue manter entre candles, e os dois foram removidos: a pausa de trezentas barras após cada operação e a regra que proíbe uma segunda entrada no mesmo dia da semana. Sem eles o diagrama entra de novo assim que o preço volta ao lado certo da média dentro da mesma janela, portanto negocia bem mais que o original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MA Period | 20 | Período da média móvel simples que confirma a direção e encerra as operações. |
| Long day from | 1 | Primeiro dia da janela comprada, como número, com domingo em zero. Um é segunda-feira. |
| Long day to | 2 | Último dia da janela comprada. Dois é terça-feira. |
| Short day from | 4 | Primeiro dia da janela vendida. Quatro é quinta-feira. |
| Short day to | 5 | Último dia da janela vendida. Cinco é sexta-feira. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta a média móvel e dois conversores, um para o preço de fechamento e outro para o dia da semana do horário de abertura.
- Quatro comparações colocam o dia dentro ou fora das duas janelas do calendário, e mais duas colocam o fechamento de um lado ou do outro da média.
- Cada E lógico une as duas pontas de uma janela, o lado da média e a checagem de posição zerada antes de acionar um bloco de entrada.
- Os dois blocos de fechamento ficam ligados diretamente às comparações com a média e trazem a condição de fechar posição, de modo que cada um zera apenas o seu lado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
