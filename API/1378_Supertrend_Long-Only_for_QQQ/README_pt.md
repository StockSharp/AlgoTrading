# Estratégia Supertrend Somente Comprado para QQQ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente comprado baseada no indicador Supertrend e um filtro de intervalo de datas.

## Detalhes

- **Critérios de entrada**: Preço cruzando acima do Supertrend.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Preço cruzando abaixo do Supertrend.
- **Stops**: Não.
- **Valores padrão**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: ATR, Supertrend
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
