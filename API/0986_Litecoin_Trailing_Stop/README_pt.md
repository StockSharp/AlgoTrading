# Estratégia de Stop Trailing para Litecoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Stop Trailing para Litecoin** usa a Média Móvel Adaptativa de Kaufman (KAMA) para detectar tendências de alta e de baixa. Abre posições compradas quando KAMA está subindo e posições vendidas quando está caindo. Após um atraso configurável, um stop trailing baseado em percentual protege os lucros.

## Detalhes
- **Critérios de entrada**: Inclinação da KAMA com resfriamento entre entradas.
- **Comprado/Vendido**: ambas as direções.
- **Critérios de saída**: stop trailing.
- **Stops**: stop trailing após o atraso.
- **Valores padrão**:
  - `KamaLength = 50`
  - `BarsBetweenEntries = 30`
  - `TrailingStopPercent = 12`
  - `DelayBars = 50`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: KAMA
  - Stops: Trailing
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
