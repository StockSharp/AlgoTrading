# Modelo de Estratégia Ultimate
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Modelo básico de cruzamento de médias móveis que abre posições compradas ou vendidas quando as médias rápida e lenta se cruzam. Inclui proteções opcionais de stop loss e take profit em percentual.

## Detalhes

- **Critérios de entrada**: SMA rápida cruzando a SMA lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto ou proteções de risco.
- **Stops**: Stop loss e take profit em percentual.
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
