# Estratégia Aprimorada de Filtro de Intervalo com ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um filtro de intervalo personalizado com níveis de take-profit e stop-loss baseados em ATR.
As entradas ocorrem quando o preço rompe o filtro e todos os filtros adicionais são satisfeitos:

- Volume acima da média
- RSI dentro dos limites configurados
- Confirmação de tendência via cruzamento de EMA
- Mercado sem consolidação baseado na proporção ATR

As posições são encerradas quando o nível de stop-loss ou take-profit baseado em ATR é atingido.
