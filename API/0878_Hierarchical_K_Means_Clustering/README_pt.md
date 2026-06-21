# Estratégia de Agrupamento Hierárquico e K-Means
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica agrupamento de volatilidade a um sistema SuperTrend. Os valores de ATR são agrupados em três clusters para determinar o regime de mercado, enquanto a direção do SuperTrend aciona as entradas. Um filtro opcional de média móvel e ADX confirma a força da tendência. As posições podem ser fechadas antecipadamente quando a razão de volume touro/urso se aproxima do equilíbrio.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SuperTrend torna-se altista && tendência do cluster > 0 && filtros aprovados.
  - **Vendido**: SuperTrend torna-se baixista && tendência do cluster < 0 && filtros aprovados.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Equilíbrio de volume ou sinal oposto.
- **Stops**: Apenas baseados em volume.
- **Valores padrão**:
  - `ATR Length` = 11.
  - `SuperTrend Factor` = 3.
  - `Training Data Length` = 200.
  - `Moving Average Length` = 50.
  - `Trend Strength Period` = 14.
  - `Trend Strength Threshold` = 20.
  - `Volume Ratio Threshold` = 0.9.
  - `Delay Bars` = 4.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
