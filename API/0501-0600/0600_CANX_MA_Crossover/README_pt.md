# Estratégia de Cruzamento de MA CANX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de EMA do preço mediano (HL2). Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta. Se o modo somente comprado estiver desativado, uma posição vendida é aberta quando a EMA rápida cruza abaixo da EMA lenta. Um filtro de ano de início impede operações antes do ano especificado.

## Parâmetros
- Tipo de candle
- Comprimento da EMA rápida
- Multiplicador (EMA lenta = comprimento rápido * multiplicador)
- Somente comprado
- Ano de início
