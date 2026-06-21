# Estratégia Pivot Point SuperTrend com Filtro de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina uma linha SuperTrend baseada em pivôs com um filtro de tendência SuperTrend e uma confirmação de média móvel. Opera quando a tendência muda ou quando um sinal de Pivot SuperTrend aparece dentro de uma janela de datas.

## Detalhes

- **Critérios de entrada**:
  - O filtro de tendência vira para cima e o preço está acima da média móvel.
  - Pivot SuperTrend emite um sinal de compra dentro do intervalo de datas configurado.
- **Critérios de saída**:
  - O filtro de tendência vira para baixo ou Pivot SuperTrend emite um sinal de venda.
- **Stops**: Nenhum
- **Valores padrão**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Pivot, SuperTrend, SMA
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Opcional
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
