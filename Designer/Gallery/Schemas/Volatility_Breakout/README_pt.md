# Diagrama da estratégia de rompimento por volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um canal construído à mão: a média móvel simples dá o centro, o Average True Range dá a largura, e um fechamento fora de SMA mais ou menos um múltiplo do ATR é tomado como um movimento em que vale a pena entrar. Como o canal respira com a volatilidade, o mesmo multiplicador continua fazendo sentido em mercados calmos e rápidos.

![schema](schema.svg)

## Visão geral da estratégia

- SMA e ATR usam o mesmo período em candles finalizados, de modo que o canal fica centrado no preço médio e escalado pela amplitude verdadeira recente.
- Dois blocos de fórmula montam as bordas: a superior é SMA mais multiplicador vezes ATR e a inferior, SMA menos a mesma quantidade.
- A estratégia está sempre no mercado: o rompimento contrário inverte a posição e um stop de proteção a fecha antes se o movimento falhar.

## Regras de entrada e saída

- **Entrada comprada**: O candle fecha acima de SMA mais multiplicador vezes ATR e a posição não está comprada. A ordem compra o volume base mais o módulo da posição: vira uma venda em compra ou abre uma compra a partir do zero.
- **Entrada vendida**: O candle fecha abaixo de SMA menos multiplicador vezes ATR e a posição não está vendida. A ordem vende o volume base mais o módulo da posição: vira uma compra em venda ou abre uma venda a partir do zero.
- **Saída**: Não há saída baseada em indicador. A posição é invertida pelo rompimento contrário ou fechada antes pelo bloco de proteção com stop loss ligado às operações das duas entradas.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Indicator period | 20 | Período compartilhado pela SMA que centraliza o canal e pelo ATR que define a sua largura. |
| ATR multiplier | 2 | A quantos ATR da média móvel fica a borda de rompimento. |
| Stop loss, % | 2 | Stop loss de proteção, em porcentagem do preço de entrada. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se o módulo da posição. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores e, por um conversor, o preço de fechamento usado tanto nas comparações quanto como fonte de preço do bloco de proteção.
- Uma constante guarda o multiplicador e dois blocos de fórmula calculam as bordas superior e inferior a partir de SMA, do multiplicador e do ATR.
- Dois blocos de comparação testam o fechamento contra as bordas, outros dois comparam a posição com zero, e cada E lógico junta uma condição de cada tipo em uma entrada.
- Um bloco de fórmula calcula o volume de inversão como volume base mais o módulo da posição e alimenta os dois blocos de modificação de posição.
- O original protege a posição com um stop de duas unidades absolutas de preço, calibrado para outro instrumento e que seria atingido de imediato em um preço de cripto; o diagrama usa no lugar um stop de dois por cento, que se comporta como o original pretendia em qualquer instrumento.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
