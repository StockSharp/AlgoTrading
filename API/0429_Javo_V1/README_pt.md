# Estratégia Javo v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Javo v1 combina velas Heikin Ashi com um par de médias móveis exponenciais. Uma posição é aberta quando a direção HA e o cruzamento do EMA rápido/lento se alinham. A abordagem tenta capturar tendências emergentes enquanto suaviza o ruído.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: HA altista e `EMA_fast > EMA_slow`
  - **Vendido**: HA baixista e `EMA_fast < EMA_slow`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Por hora
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
