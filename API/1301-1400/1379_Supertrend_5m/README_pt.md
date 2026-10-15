# Estratégia Supertrend (5m)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Supertrend em velas de 5 minutos.

## Detalhes

- **Critérios de entrada**: Preço cruzando acima do Supertrend.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Preço cruzando abaixo do Supertrend.
- **Stops**: Não.
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: ATR, Supertrend
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
