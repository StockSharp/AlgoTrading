# Estratégia Geo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compra quando a relação máxima/mínima da vela está próxima da proporção áurea.

## Detalhes

- **Critérios de entrada**: Relação máximo/mínimo dentro da tolerância de phi.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Condição oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `Tolerance` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candle ratio
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
