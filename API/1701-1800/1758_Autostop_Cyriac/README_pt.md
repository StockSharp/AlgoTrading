# Estratégia Autostop Cyriac
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utilitária anexa automaticamente um take profit e um stop loss a cada negociação enquanto está ativa. Ela não cria entradas ou saídas por si mesma e pode ser combinada com trading manual ou outras estratégias.

## Detalhes

- **Critérios de entrada**: Nenhum. As posições são abertas manualmente ou por lógica externa.
- **Comprado/Vendido**: Posições compradas e vendidas são suportadas.
- **Critérios de saída**: As posições são fechadas pelo take profit ou stop loss anexados.
- **Stops**: Sim. Offsets de preço absolutos para take profit e stop loss via `StartProtection`.
- **Filtros**: Nenhum.

A estratégia expõe dois parâmetros:

- `TakeProfit` – distância até o take profit em unidades de preço.
- `StopLoss` – distância até o stop loss em unidades de preço.
