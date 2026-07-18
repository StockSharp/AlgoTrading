# Estratégia Aprimorada de Bollinger Bands com SL TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera rebotes nas Bollinger Bands usando ordens limitadas e stop-loss e take-profit fixos baseados em pips.

## Detalhes

- **Critérios de entrada**:
  - Comprado: fechamento anterior <= banda inferior anterior e fechamento > banda inferior
  - Vendido: fechamento anterior >= banda superior anterior e fechamento < banda superior
- **Comprado/Vendido**: Ambos
- **Stops**: Take-profit e stop-loss absolutos em pips
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
