# Estratégia Coensio Swing Trader V06
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica de rompimento do Coensio Swing Trader original. Usa o Canal de Donchian para definir suporte e resistência dinâmicos. Uma operação é aberta quando o preço rompe acima da banda superior ou abaixo da banda inferior por um limite configurável.

## Detalhes

- **Entrada**:
  - **Comprado**: O preço de fechamento rompe acima da banda superior do Canal de Donchian + `Entry Threshold` pips.
  - **Vendido**: O preço de fechamento rompe abaixo da banda inferior do Canal de Donchian - `Entry Threshold` pips.
- **Saídas**:
  - `Stop Loss` e `Take Profit` fixos em pips medidos a partir do preço de entrada.
  - Movimento opcional para ponto de equilíbrio após `Break Even` pips de lucro.
  - Trailing stop opcional que segue o preço por `Trailing Step` pips após o ponto de equilíbrio.
- **Stops**: Stop-loss, take-profit, ponto de equilíbrio, trailing stop.
- **Valores padrão**:
  - `Channel Period` = 20
  - `Entry Threshold` = 15 pips
  - `Stop Loss` = 50 pips
  - `Take Profit` = 80 pips
  - `Break Even` = 25 pips
  - `Trailing Step` = 5 pips
  - `Enable Trailing` = false
  - `Candle Type` = velas de 15 minutos
