# Estratégia OBVious MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre uma posição comprada quando o OBV cruza acima da sua média móvel de entrada longa e sai quando o OBV cruza abaixo da média de saída longa. Posições vendidas são abertas quando o OBV cruza abaixo da sua média de entrada curta e fechadas quando cruza acima da média de saída curta. Um filtro de direção permite habilitar apenas operações compradas ou vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: OBV cruza acima da MA de entrada longa e a direção não é Short.
  - **Vendido**: OBV cruza abaixo da MA de entrada curta e a direção não é Long.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: OBV cruza abaixo da MA de saída longa.
  - Vendido: OBV cruza acima da MA de saída curta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LongEntryLength` = 190
  - `LongExitLength` = 202
  - `ShortEntryLength` = 395
  - `ShortExitLength` = 300
  - `TradeDirection` = "Long"
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: OBV, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
