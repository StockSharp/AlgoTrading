# Estratégia de Média Móvel Simples em Múltiplos Períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica de `simple_multiple_time_frame_moving_average.mq4`. Ela alinha tendências em dois períodos utilizando médias móveis simples.

## Lógica da Estratégia
- Calcula SMA com período `Length` em velas de 1 hora e 4 horas.
- Entra comprado quando ambas as SMAs estão subindo.
- Entra vendido quando ambas as SMAs estão caindo.
- Fecha uma posição comprada quando qualquer SMA vira para baixo.
- Fecha uma posição vendida quando qualquer SMA vira para cima.
- Apenas uma posição pode estar ativa por vez.

## Parâmetros
- **MA Length** (`Length`): período utilizado para ambas as médias móveis.
- **Short Time Frame** (`ShortCandleType`): período para a primeira SMA (padrão: 1 hora).
- **Long Time Frame** (`LongCandleType`): período para a segunda SMA (padrão: 4 horas).

O volume de negociação é obtido da propriedade `Volume` da estratégia.

## Notas
Esta implementação foca nas médias de uma hora e quatro horas usadas na versão MQL original e omite os cálculos de períodos superiores não utilizados.
