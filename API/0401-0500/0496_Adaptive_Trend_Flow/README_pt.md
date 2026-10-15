# Estratégia de Fluxo de Tendência Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Fluxo de Tendência Adaptativa constrói um canal baseado em volatilidade a partir de EMAs rápidas e lentas do preço típico. Quando o preço cruza os limites do canal, a tendência interna muda. Posições compradas são abertas quando a tendência vira para cima e os filtros opcionais de SMA e MACD confirmam. As posições são fechadas quando a tendência reverte para baixo.

## Detalhes

- **Critérios de entrada**:
  - A tendência muda de baixa para alta e os filtros confirmam.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - A tendência muda de alta para baixa.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA, SMA, MACD, Standard Deviation
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
