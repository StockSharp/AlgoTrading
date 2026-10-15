# Estratégia de Sinais AI da ThinkTech
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos da primeira vela de 15 minutos da sessão. Utiliza níveis de stop loss e take profit baseados em ATR e pode aplicar filtros opcionais de tendência e RSI.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço rompe acima da máxima da primeira vela com os filtros de tendência e RSI satisfeitos.
  - **Vendido**: O preço rompe abaixo da mínima da primeira vela com os filtros de tendência e RSI satisfeitos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Atingir o nível de take profit ou stop loss.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
