# Estratégia Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula o Z-Score de uma EMA de Heikin-Ashi e opera com base em cruzamentos de limiares dinâmicos derivados de ranges recentes.

## Detalhes

- **Critérios de entrada**: Score cruzando acima da mínima recente ou EMA do score cruzando acima da faixa média
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: EMA do score cruzando abaixo da máxima ou mínima recente
- **Stops**: Não
- **Valores padrão**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: EMA, SMA, StdDev, Highest, Lowest
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
