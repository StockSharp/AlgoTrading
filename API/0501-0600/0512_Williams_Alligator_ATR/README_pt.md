# Estratégia Williams Alligator ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador Williams Alligator combinado com um stop-loss baseado em ATR. Uma posição comprada é aberta quando a linha Lips cruza acima da linha Jaw. A posição é fechada quando Lips cruza abaixo de Jaw ou quando o preço cai até um nível de stop baseado em ATR.

## Detalhes
- **Critérios de entrada**: Lips cruza acima de Jaw.
- **Critérios de saída**: Lips cruza abaixo de Jaw ou stop-loss por ATR.
- **Indicadores**: Smoothed Moving Averages, Average True Range.
- **Tipo**: Somente comprado.
