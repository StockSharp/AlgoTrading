# Estratégia de MA Ponderada por Volume com Desvio Padrão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica uma Média Móvel Ponderada por Volume (VWMA) com um filtro de desvio padrão. Mede o momentum da VWMA e abre uma posição comprada quando o movimento ascendente excede um limiar de desvio configurável. Uma posição vendida é aberta quando o movimento descendente cruza o limiar negativo. A abordagem tenta capturar movimentos direcionais fortes confirmados pelo volume.

## Parâmetros
- Tipo de vela
- Comprimento da VWMA
- Período de StdDev
- K1
- K2
