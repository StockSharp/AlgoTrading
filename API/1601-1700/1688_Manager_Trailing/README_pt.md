# Estratégia de Gestão com Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma única posição comprada e a gerencia usando vários controles de risco:

- **Take profit** e **stop loss** baseados em percentual.
- **Trailing** de lucro que se ativa após um ganho configurável.
- **Fechamento parcial** em níveis de lucro personalizados.

O algoritmo demonstra como gerenciar uma posição existente com StockSharp usando apenas dados de candles.

## Detalhes

- **Critérios de entrada**: Compra a mercado no primeiro candle concluído.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Percentual de take profit.
  - Percentual de stop loss.
  - Gatilho de trailing de lucro.
  - Porções de fechamento parcial.
- **Stops**: Sim, via percentuais.
- **Filtros**: Nenhum.
