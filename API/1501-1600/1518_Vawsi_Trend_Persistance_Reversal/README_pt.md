# Estratégia de Reversão VAWSI e Persistência de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão que combina VAWSI, persistência de tendência e ATR para construir um limiar dinâmico em velas Heikin-Ashi.

## Detalhes

- **Critérios de entrada**: Fechamento Heikin-Ashi cruza acima/abaixo do limiar dinâmico
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento oposto ou stops de proteção
- **Stops**: Sim, baseados em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
