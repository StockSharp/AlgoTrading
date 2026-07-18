# Estratégia de Inclinação de MA Ponderada por Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Inclinação de MA Ponderada por Volume** analisa a direção da Média Móvel Ponderada por Volume (VWMA). O sistema entra em uma posição comprada quando a VWMA sobe por duas barras consecutivas e abre uma posição vendida quando a VWMA cai por duas barras. As posições existentes são fechadas assim que a inclinação do indicador se reverte.

Esta abordagem tenta seguir tendências emergentes usando médias de preço ajustadas por volume, filtrando movimentos que ocorrem em baixo volume.

## Detalhes

- **Critérios de entrada**: VWMA subindo por duas barras (comprado) ou caindo por duas barras (vendido).
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Inclinação oposta da VWMA.
- **Stops**: Sim (configurável, padrão stop loss 1% / take profit 2%).
- **Valores padrão**:
  - `VwmaPeriod` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: VWMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
