# Estratégia de Divergência AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia busca divergências de alta e de baixa entre o Awesome Oscillator (AO) e o preço. Uma divergência de alta ocorre quando o preço forma uma mínima mais baixa enquanto o AO forma uma mínima mais alta. Uma divergência de baixa aparece quando o preço forma uma máxima mais alta enquanto o AO forma uma máxima mais baixa.

Quando uma divergência de alta é detectada, a estratégia abre uma posição comprada. Uma divergência de baixa aciona uma posição vendida. As posições se invertem com sinais opostos.

## Detalhes

- **Critérios de entrada**: Divergência de alta ou baixa do AO com o preço.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal de divergência oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 5 minutos
  - `FastLength` = 5
  - `SlowLength` = 34
  - `Lookback` = 5
  - `UseEma` = false
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: Awesome Oscillator
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
