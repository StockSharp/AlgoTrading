# Estratégia de Pullback na Mínima de 10 Barras Somente Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra vendida quando o preço rompe a mínima mais baixa das barras anteriores e a força interna da barra (IBS) está acima de um limiar. Um filtro EMA opcional confirma a tendência de baixa.

## Detalhes

- **Critérios de entrada**:
  - A mínima rompe a mínima mais baixa das `LowestPeriod` barras anteriores.
  - IBS > `IbsThreshold`.
  - Opcional: preço de fechamento abaixo da EMA quando o filtro está ativado.
  - Hora entre `StartTime` e `EndTime`.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**:
  - Preço de fechamento abaixo da mínima anterior fecha a posição vendida.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoria: Pullback
  - Direção: Vendido
  - Indicadores: Lowest, EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
