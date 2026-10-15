# Estratégia Renko Live Charts Pimped
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói tijolos Renko e opera nas mudanças de direção. Opcionalmente pode calcular o tamanho do tijolo a partir de valores ATR, permitindo que a estrutura Renko se adapte à volatilidade do mercado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: tijolo Renko de alta após um de baixa.
  - **Vendido**: tijolo Renko de baixa após um de alta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal reverso.
- **Stops**: Não.
- **Valores padrão**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Renko, ATR
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Renko
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
