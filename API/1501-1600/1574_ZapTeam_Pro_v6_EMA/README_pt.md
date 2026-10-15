# Estratégia ZapTeam Pro v6 — EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Versão simplificada usando cruzamento de EMA21/EMA50 com filtro de tendência EMA200. Compra no cruzamento de alta e vende no cruzamento de baixa (vendidos opcionais).

## Detalhes

- **Critérios de entrada**: EMA21 cruza EMA50 com filtro de tendência
- **Comprado/Vendido**: Ambos (vendidos opcionais)
- **Critérios de saída**: Cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `Ema21Length` = 21
  - `Ema50Length` = 50
  - `Ema200Length` = 200
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
