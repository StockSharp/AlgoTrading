# Estratégia de 10 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de hedge abre posições compradas e vendidas ao mesmo tempo. Cada posição usa níveis fixos de take-profit e stop-loss medidos em unidades de preço e pode ser protegida por um trailing stop. Quando um lado fecha, a estratégia abre imediatamente uma nova posição na mesma direção para manter ambos os lados ativos.

## Parâmetros
- `TakeProfitBuy` – distância de take-profit para posições compradas.
- `StopLossBuy` – distância de stop-loss para posições compradas.
- `TrailingStopBuy` – distância de trailing stop para posições compradas.
- `TakeProfitSell` – distância de take-profit para posições vendidas.
- `StopLossSell` – distância de stop-loss para posições vendidas.
- `TrailingStopSell` – distância de trailing stop para posições vendidas.
- `Volume` – tamanho da ordem usado para todas as operações.

## Notas
- As posições são abertas com ordens de mercado.
- As ordens de proteção são registradas para cada lado separadamente.
- Os trailing stops são atualizados quando o mercado se move em uma direção favorável.
