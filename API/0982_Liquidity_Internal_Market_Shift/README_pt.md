# Estratégia de Mudança Interna de Mercado de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta mudanças na estrutura interna do mercado que coincidem com varreduras de liquidez em máximas ou mínimas recentes. Uma operação é aberta quando o preço toca uma linha de liquidez e então muda a estrutura na direção oposta. A negociação pode ser limitada apenas a configurações de alta, de baixa ou ambas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço fecha acima da estrutura baixista anterior e tocou a linha de liquidez da mínima recente.
  - **Vendido**: O preço fecha abaixo da estrutura altista anterior e tocou a linha de liquidez da máxima recente.
- **Comprado/Vendido**: Ambas as direções ou selecionável Apenas Alta / Apenas Baixa.
- **Critérios de saída**:
  - Sinal oposto após a entrada.
  - Stop-loss em `StopLossPips` pips.
  - Take-profit opcional em `TakeProfitPips` pips.
- **Stops**: Sim, stop-loss configurável e take-profit opcional.
- **Filtros**:
  - Opera apenas dentro do intervalo de tempo especificado.
  - O bloqueio de sinal impede entradas repetidas por várias barras.
