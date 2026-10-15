# Estratégia MOC Delta MOO Entry
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula o delta de volume de compra e venda durante a sessão de 14:50–14:55 e opera às 08:30 se o percentual do delta ultrapassar um limiar relativo ao volume do dia. Utiliza filtros SMA no preço de abertura e aplica stop loss e take profit baseados em ticks.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** 08:30, delta MOC % acima do limiar, abertura acima do SMA15 e SMA30.
  - **Vendido:** 08:30, delta MOC % abaixo do limiar negativo, abertura abaixo do SMA15 e SMA30.
- **Critérios de saída:**
  - **Stops:** Take profit e stop loss em ticks.
  - **Por tempo:** Encerramento de todas as posições às 14:50.
- **Valores padrão:**
  - `DeltaThreshold` = 2
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 10
