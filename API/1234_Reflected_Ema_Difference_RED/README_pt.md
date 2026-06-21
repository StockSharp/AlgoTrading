# Estratégia de Diferença EMA Refletida RED
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reflete a distância entre duas Hull Moving Averages e acompanha um valor suavizado. Quando o reflexo suavizado se reverte em um percentual especificado, ela entra em posições compradas ou vendidas conforme o caso.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o reflexo suavizado sobe acima do seu limite de retração.
  - Vendido: o reflexo suavizado cai abaixo do seu limite de retração.
- **Comprado/Vendido**: Ambos
- **Valores padrão**:
  - `Smoothing Period` = 2
  - `Change Percent` = 0.04
