# Estratégia de Price Action
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Price Action** alterna entre ordens a mercado compradas e vendidas sempre que a posição anterior é fechada.
Aplica uma distância de stop-loss fixa, um alvo de take-profit baseado em alavancagem e um stop de rastreamento opcional que segue o mercado com um passo configurável.

## Detalhes
- **Critérios de entrada:** Sem posição aberta. A direção alterna entre compra e venda após cada operação.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:** O preço atinge o stop de rastreamento, o stop inicial ou o nível de take-profit.
- **Stops:** Distância de stop fixa com rastreamento opcional (o passo define o movimento mínimo de preço para atualização).
- **Valores padrão:** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`.
