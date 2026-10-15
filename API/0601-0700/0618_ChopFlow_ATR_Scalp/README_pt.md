# Scalp ChopFlow ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

ChopFlow ATR Scalp entra quando o mercado sai de condições laterais e o OBV cruza sua EMA. As saídas usam stops e alvos simétricos baseados em ATR.

O objetivo é capturar movimentos rápidos durante a formação inicial de tendências.

## Detalhes

- **Critérios de entrada**: `Choppiness < ChopThreshold` e OBV acima/abaixo de sua EMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop ou distância de take-profit baseada em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Scalping
  - Direção: Ambos
  - Indicadores: ATR, Choppiness, OBV
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
