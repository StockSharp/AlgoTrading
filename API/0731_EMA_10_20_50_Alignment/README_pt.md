# Alinhamento EMA 10/20/50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia somente comprado entra quando EMA(10) > EMA(20) > EMA(50) e sai quando as EMAs se alinham em ordem descendente. O trading é restrito a um intervalo de datas configurável.

## Detalhes

- **Critérios de entrada**: EMA(10) acima de EMA(20) acima de EMA(50) dentro do intervalo de datas especificado.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: EMAs se alinham para baixo (EMA(10) < EMA(20) < EMA(50)).
- **Stops**: Não.
- **Valores padrão**:
  - `StartTime` = new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `EndTime` = new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
