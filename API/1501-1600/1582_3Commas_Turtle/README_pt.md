# Estratégia 3Commas Turtle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento simplificado no estilo Turtle usando canais Donchian. Compra em rompimentos acima do canal rápido quando o preço também está acima do canal lento, e vende em quedas abaixo do canal rápido com confirmação do canal lento. As saídas ocorrem quando o preço cruza o canal de saída na direção oposta.

## Detalhes
- **Critérios de entrada**: Rompimento do canal rápido com confirmação do canal lento.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Preço cruza o canal de saída.
- **Stops**: Baseados no canal.
- **Valores padrão**:
  - `PeriodFast` = 20
  - `PeriodSlow` = 20
  - `PeriodExit` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Canais Donchian
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
