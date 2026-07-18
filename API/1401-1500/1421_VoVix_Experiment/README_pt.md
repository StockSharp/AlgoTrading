# Estratégia do Experimento VoVix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia analisa a razão entre o ATR rápido e o ATR lento. Quando o z-score dessa razão dispara e atinge um máximo local, entra na direção do candle. As posições são fechadas quando o z-score cai abaixo do limiar de saída.

## Detalhes

- **Critérios de entrada**: Z-score do VoVix acima de `EntryZ` e no máximo local
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Z-score do VoVix abaixo de `ExitZ`
- **Stops**: Não
- **Valores padrão**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: ATR, Highest, SMA, StdDev
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
