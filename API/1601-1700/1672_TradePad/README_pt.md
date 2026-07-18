# Estratégia TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia TradePad é um painel de trading manual portado do especialista MQL TradePad original. A estratégia configura um painel para gerenciar trades de forma interativa. Processa dados de tick, notificações de trades, eventos de temporizador e mensagens de gráfico sem regras automáticas de entrada ou saída.

Este exemplo demonstra como construir uma interface de trading discricionário sobre o StockSharp.

## Detalhes

- **Critérios de entrada**: Colocação manual de ordens pelo painel.
- **Comprado/Vendido**: Ambos, dependendo da ação do usuário.
- **Critérios de saída**: Fechamento manual da posição.
- **Stops**: Nenhum; o usuário pode implementar lógica personalizada.
- **Filtros**: Sem filtros automáticos.
