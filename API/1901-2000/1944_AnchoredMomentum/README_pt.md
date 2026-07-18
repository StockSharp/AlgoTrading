# AnchoredMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AnchoredMomentum calcula a relação entre a EMA e a SMA dos preços de fechamento dos candles. Quando o momentum sobe acima de um limiar superior, abre posições compradas; quando cai abaixo de um limiar inferior, abre posições vendidas. Sinais opostos fecham as posições atuais.

## Detalhes

- **Critérios de entrada**: O momentum cruza acima de `UpLevel` para comprado, abaixo de `DownLevel` para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O sinal oposto fecha a posição.
- **Stops**: Não.
- **Valores padrão**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025m
  - `DownLevel` = -0.025m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
