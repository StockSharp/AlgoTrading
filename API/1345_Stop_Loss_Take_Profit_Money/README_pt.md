# Estratégia de Stop Loss e Take Profit em Dinheiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprada quando uma SMA de curto prazo cruza acima de uma SMA de longo prazo e vendida no cruzamento oposto. As posições são fechadas quando o lucro ou a perda atinge valores monetários predefinidos.

## Detalhes

- **Critérios de entrada**: SMA(14) cruza SMA(28)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O lucro ou a perda em dinheiro atinge o alvo
- **Stops**: Sim
- **Valores padrão**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100
