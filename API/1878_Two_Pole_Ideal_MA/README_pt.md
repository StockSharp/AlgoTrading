# Estratégia Two-Pole Ideal MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de cruzamento que aproxima o especialista "2pb Ideal MA" comparando uma EMA rápida com uma TEMA lenta.

## Detalhes

- **Critérios de entrada**: EMA rápida cruzando TEMA lenta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Reversão no cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, TEMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Swing (H4)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
