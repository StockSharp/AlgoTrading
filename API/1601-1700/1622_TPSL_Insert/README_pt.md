# Estratégia TPSL Insert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução StockSharp do script MetaTrader 4 **TPSL-Insert.mq4**. Ela não gera sinais de entrada ou saída. Seu único propósito é anexar ordens de take profit e stop loss a posições existentes.

## Como funciona

1. No início, a estratégia lê os parâmetros `TakeProfitPips` e `StopLossPips`.
2. Os valores são convertidos de pips para preço usando o `PriceStep` do instrumento.
3. `StartProtection` é chamado para colocar ordens protetoras.
   - Se uma posição já existir, as ordens protetoras são enviadas imediatamente.
   - Posições futuras abertas pela estratégia serão protegidas automaticamente.

Este comportamento é útil quando posições são abertas manualmente ou por sistemas externos e você precisa inserir limites de risco rapidamente.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `TakeProfitPips` | Distância de take profit em pips. | `35` |
| `StopLossPips` | Distância de stop loss em pips. | `100` |

## Observações

- A estratégia não se inscreve em dados de mercado e não contém lógica de negociação.
- `StartProtection` lida com a criação e cancelamento de ordens protetoras.
