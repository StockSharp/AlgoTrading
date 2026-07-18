# Estratégia Molly ETF EMA Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra em uma posição comprada quando a EMA rápida cruza acima da EMA lenta e sai quando a EMA rápida cruza abaixo. Inclui parâmetros opcionais para restringir a negociação a um intervalo de datas específico.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A EMA rápida cruza acima da EMA lenta dentro do intervalo de datas.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - A EMA rápida cruza abaixo da EMA lenta ou o intervalo de datas termina.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fast EMA` = 10
  - `Slow EMA` = 21
  - `Start Date` = 2018-01-01
  - `End Date` = 2023-09-07
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
