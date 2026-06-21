# Estratégia Fibonacci ATR Fusion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina índices de pressão compradora em múltiplos períodos Fibonacci com ATR e usa cruzamentos de limiar para entradas e saídas. Take-profit em camadas baseado em ATR opcional.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A média ponderada cruza acima de `LongEntryThreshold`.
  - **Vendido**: A média ponderada cruza abaixo de `ShortEntryThreshold`.
- **Critérios de saída**:
  - A média ponderada cruza os limiares de saída opostos ou reversão de posição.
- **Indicadores**:
  - Índices ponderados de pressão compradora sobre ATR.
  - ATR para take-profit opcional.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: ATR
  - Stops: nenhum
  - Complexidade: Moderado
