# Diagrama da estratégia de rompimento de N dias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O clássico das tartarugas reduzido ao essencial: dois indicadores, Highest e Lowest, guardam os extremos das últimas N barras, e o candle que ultrapassa qualquer um deles é tomado como o início de um movimento. O diagrama está sempre no mercado e inverte no rompimento contrário.

![schema](schema.svg)

## Visão geral da estratégia

- Highest lê a máxima de cada candle finalizado e Lowest lê a mínima, de modo que juntos formam o canal de rompimento do período de observação.
- As duas leituras são deslocadas um candle para trás, pois o valor atual já inclui o candle que está sendo testado: sem o deslocamento a máxima, no máximo, igualaria o canal e nunca o superaria.
- A posição atual libera ou bloqueia cada entrada, e ao volume da ordem soma-se o módulo da posição, assim uma única ordem a mercado inverte o lado.

## Regras de entrada e saída

- **Entrada comprada**: A máxima do candle supera o valor de Highest do candle anterior e a posição não está comprada. A ordem compra o volume base mais o módulo da posição: vira uma venda em compra ou abre uma compra a partir do zero.
- **Entrada vendida**: A mínima do candle cai abaixo do valor de Lowest do candle anterior, o rompimento de alta não disparou no mesmo candle e a posição não está vendida. A ordem vende o volume base mais o módulo da posição.
- **Saída**: Sem stop, sem alvo e sem saída própria: a posição permanece até que o rompimento contrário a inverta, tal como faz o código original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Lookback period | 20 | Número de barras sobre as quais o canal de rompimento é construído; o mesmo comprimento vale para Highest e Lowest. |
| Volume | 1 | Volume base da ordem, em lotes; na inversão soma-se o módulo da posição. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os dois indicadores e, por dois conversores, a máxima e a mínima do candle atual.
- Dois blocos de valor anterior atrasam as leituras de Highest e Lowest em um candle, e nisso está todo o truque desta estratégia.
- Os blocos de comparação produzem as duas bandeiras de rompimento e outros dois comparam a posição com zero; um NÃO lógico dá prioridade ao rompimento de alta sobre o de baixa, como o ramo else-if do original.
- Um bloco de fórmula calcula o volume de inversão como volume base mais o módulo da posição e alimenta os dois blocos de modificação de posição.
- O original declara uma média móvel e um percentual de stop que o próprio código nunca usa, e adota por padrão um canal de 1500 barras de um minuto; o diagrama descarta esses parâmetros mortos e usa um canal de 20 barras de cinco minutos, como sugerem o README da estratégia e a sua faixa de otimização.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
