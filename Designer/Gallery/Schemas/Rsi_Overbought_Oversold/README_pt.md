# Diagrama da estratégia de sobrecompra e sobrevenda do RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um diagrama clássico de reversão à média: o índice de força relativa mede o quanto o movimento recente se esticou, e a estratégia se posiciona contra ele quando o índice atinge um extremo. A verificação da posição impede o acúmulo de operações no mesmo sentido.

![schema](schema.svg)

## Visão geral da estratégia

- O índice de força relativa é calculado sobre candles finalizados de um único instrumento.
- Dois limiares delimitam as zonas: abaixo do nível de sobrevenda o mercado é considerado liquidado; acima do nível de sobrecompra, esticado.
- A posição atual participa de cada decisão, de modo que a entrada só ocorre quando a ordem não aumenta uma posição já aberta.

## Regras de entrada e saída

- **Entrada comprada**: O RSI está no nível de sobrevenda ou abaixo dele e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: O RSI está no nível de sobrecompra ou acima dele e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: Não há bloco de saída próprio: o sinal contrário fecha a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| RSI Length | 14 | Período de suavização do índice de força relativa. |
| Oversold | 30 | Nível em que, ou abaixo do qual, o índice é considerado sobrevendido. |
| Overbought | 70 | Nível em que, ou acima do qual, o índice é considerado sobrecomprado. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador que contém o índice de força relativa.
- Dois blocos de comparação testam o índice contra as constantes de limiar; outros dois comparam a posição com zero.
- Cada E lógico une uma condição do índice a uma da posição e aciona um bloco de modificação de posição.
- Ambos os blocos de modificação enviam ordens a mercado e obtêm o volume de uma constante compartilhada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
