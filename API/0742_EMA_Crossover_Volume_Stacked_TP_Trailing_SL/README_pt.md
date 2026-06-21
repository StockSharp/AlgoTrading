# Estratégia de Cruzamento EMA com Volume + TP Escalonado e SL Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de EMA filtrados por volume. Define dois alvos de lucro baseados em ATR e aplica um trailing stop à posição restante quando o preço se move favoravelmente.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima/abaixo da EMA lenta.
  - Volume > volume médio * `VolumeMultiplier`.
- **Comprado/Vendido**: Comprado e Vendido.
- **Critérios de saída**:
  - Primeiro take profit em `TP1Multiplier * ATR` (33% da posição).
  - Segundo take profit em `TP2Multiplier * ATR` (outros 33%).
  - O trailing stop é ativado após o preço se mover `TrailTriggerMultiplier * ATR` e segue a `TrailOffsetMultiplier * ATR`.
- **Stops**: Apenas trailing stop.
- **Valores padrão**:
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: EMA, ATR, Volume
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
