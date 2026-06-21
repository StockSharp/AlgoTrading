# Estratégia Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando um tijolo Renko de alta segue um de baixa e entra vendido na mudança oposta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: tijolo Renko de alta após um tijolo de baixa.
  - **Vendido**: tijolo Renko de baixa após um tijolo de alta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal inverso.
- **Stops**: Não.
- **Valores padrão**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CandleType` = Renko.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Renko
  - Stops: Não
  - Complexidade: Básico
  - Período: Renko
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
