# Estratégia de Opções V1.3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de cruzamento de EMA com RSI, stop e take-profit baseados em ATR, e filtro de volume. O sistema pode opcionalmente exigir um rompimento do intervalo de abertura e fecha posições às 15:55 horário de Nova York. As operações são bloqueadas durante sessões predefinidas e um intervalo de não operação especificado pelo usuário.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA curta cruza acima da EMA longa, RSI ≥ `RsiLongThreshold`, volume ≥ SMA do volume, opcionalmente fechamento > máxima do intervalo de abertura.
  - **Vendido**: EMA curta cruza abaixo da EMA longa, RSI ≤ `RsiShortThreshold`, volume ≥ SMA do volume, opcionalmente fechamento < mínima do intervalo de abertura.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss e take-profit baseados em ATR.
  - Cruzamento oposto de EMA.
  - Fechamento automático às 15:55 horário NY.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: EMA, RSI, ATR, SMA
  - Stops: Sim
  - Período: Intradiário
