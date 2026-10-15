# Estratégia Honest Volatility Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em múltiplos níveis do Keltner Channel para construir uma grade de volatilidade. Escala em posições compradas e vendidas em bandas predefinidas e sai por níveis opostos ou um stop bruto.

## Detalhes

- **Critérios de entrada**: O preço atinge os níveis configurados do canal Keltner.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Canal oposto ou stop bruto.
- **Stops**: Stop bruto opcional.
- **Valores padrão**:
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
