# Estratégia VIDYA ProTrend de Lucro Multi-Nível
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência usando médias VIDYA rápidas e lentas com um filtro de Bandas de Bollinger.
Opcionalmente, ordens de take profit em múltiplos níveis são colocadas usando múltiplos de ATR e metas percentuais.

## Detalhes

- **Critérios de entrada**: VIDYA rápida acima da VIDYA lenta com preço fora do filtro de Bollinger
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: inclinação ou cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: VIDYA, Bollinger Bands, ATR
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
