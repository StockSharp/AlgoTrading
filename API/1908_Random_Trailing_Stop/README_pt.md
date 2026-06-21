# Estratégia de Trailing Stop Aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Trailing Stop Aleatório abre operações aleatórias com viés determinado por uma média móvel simples e as gerencia usando um trailing stop.

## Detalhes

- **Critérios de entrada**: direção aleatória com viés de SMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: trailing stop
- **Stops**: Sim
- **Valores padrão**:
  - `MinStopLevel` = 0.00036
  - `TrailingStep` = 0.00001
  - `SleepMinutes` = 5
  - `SmaPeriod` = 100
  - `Volume` = 0.1
- **Filtros**:
  - Categoria: Experimental
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: 1m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
