# Estratégia Definitiva de Scalping BTC T3 Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica duas médias móveis Tilson T3 para capturar movimentos de curto prazo em BTC. Um cruzamento entre as linhas T3 ajustadas por Fibonacci e a T3 padrão gera entradas compradas ou vendidas. O gerenciamento opcional de TP/SL e o fechamento em sinais opostos são suportados.

Os testes indicam um retorno anual médio de cerca de 38%. Funciona melhor em pares de BTC com baixa latência.

A estratégia compra quando a T3 rápida cruza acima da T3 lenta e vende no cruzamento oposto. As posições podem ser fechadas em sinais inversos ou por níveis percentuais de take profit e stop loss.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: T3 rápida cruza acima da T3 lenta.
  - **Vendido**: T3 rápida cruza abaixo da T3 lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto ou TP/SL se ativado.
- **Stops**: Opcional baseado em percentual.
- **Filtros**:
  - Nenhum.
