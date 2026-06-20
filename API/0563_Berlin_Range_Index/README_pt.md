# Estratégia Berlin Range Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Berlin Range Index filtra o Índice de Choppiness padrão com um fator baseado em ATR para destacar fases de tendência e de range. Quando o índice filtrado cai abaixo de um limiar mínimo, a estratégia abre uma posição na direção do candle atual. As posições são fechadas quando o índice indica uma fase de range ou tendência enfraquecida.

## Detalhes

- **Critérios de entrada**:
  - Índice de range filtrado abaixo de `ChopMin` e a direção do candle define comprado ou vendido.
- **Critérios de saída**:
  - Índice de range acima de `ChopMax` ou tendência enfraquecida.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Choppiness Index, ATR, Standard Deviation
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
