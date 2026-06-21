# Estratégia de Rastreamento com Canal de Reversão à Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera em um canal de reversão à média construído a partir de uma média móvel e o ATR. Vende quando o preço fecha acima da banda superior e compra quando o preço fecha abaixo da banda inferior. As posições são encerradas quando o preço retorna à linha média.

## Detalhes
- Entrada: fechamento acima da banda superior -> vendido, fechamento abaixo da banda inferior -> comprado.
- Saída: preço retornando à média.
- Indicadores: SMA e ATR.
- Stops: nenhum.
