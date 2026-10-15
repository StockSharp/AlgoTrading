# Estratégia Ultimate Balance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Ultimate Balance combina ROC, RSI, CCI, Williams %R e ADX em um oscilador ponderado. Uma média móvel desse oscilador gera sinais: cruzar acima do nível de sobrevenda aciona um comprado, enquanto cruzar abaixo do nível de sobrecompra encerra ou reverte a posição.

## Detalhes

- **Critérios de entrada**: MA do oscilador cruza acima de `OversoldLevel`.
- **Comprado/Vendido**: Ambos (vendido opcional via `EnableShort`).
- **Critérios de saída**: MA do oscilador cruza abaixo de `OverboughtLevel`.
- **Stops**: Não.
- **Valores padrão**:
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: ROC, RSI, CCI, WilliamsR, ADX
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
