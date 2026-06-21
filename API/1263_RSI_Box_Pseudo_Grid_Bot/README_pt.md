# RSI Box (Bot de Pseudo Grade)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em grade que deriva níveis de preço a partir de sinais de sobrecompra e sobrevenda do RSI. Quando o RSI cruza um extremo, as linhas de grade dinâmicas são recalculadas a partir de máximos e mínimos recentes. As operações ocorrem quando o preço rompe acima ou abaixo do próximo nível de grade. Posições vendidas são opcionais.

## Detalhes

- **Critérios de entrada**: O preço cruza a próxima linha de grade após um extremo do RSI.
- **Comprado/Vendido**: Comprado por padrão, vendidos opcionais.
- **Critérios de saída**: O preço cruza a linha de grade oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `UseShorts` = false
- **Filtros**:
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: RSI, Highest, Lowest
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
