# Estratégia Falcon Liquidity Grab
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera capturas de liquidez durante as principais sessões de mercado usando uma média móvel simples para definir a tendência. Entra quando o preço ultrapassa níveis de swing recentes e reverte com a tendência. Cada operação usa stop loss e take profit fixos medidos em ticks.

## Detalhes

- **Condições de entrada**:
  - **Comprado**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **Vendido**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **Condições de saída**: stop loss e take profit fixos.
- **Tipo**: Reversão
- **Indicadores**: SMA, Highest, Lowest
- **Período**: 15 minutos (padrão)
- **Stops**: `StopLossPoints` ticks, `TakeProfitMultiplier`× distância do stop
