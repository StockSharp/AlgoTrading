# TF Estratégia de Regressão Linear Segmentada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aplica um canal de regressão linear dentro de cada segmento de tempo. Uma posição comprada é aberta quando o preço cruza acima da banda superior e uma vendida quando cruza abaixo da banda inferior.

## Detalhes
- **Critérios de entrada**: O preço cruza o canal de regressão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento da banda oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Linear Regression
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
