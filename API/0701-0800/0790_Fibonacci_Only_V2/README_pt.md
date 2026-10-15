# Estratégia Exclusiva Fibonacci V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera em torno das retrações Fibonacci de 19% e 82,56% calculadas sobre 93 candles. As entradas ocorrem quando o preço toca ou rompe esses níveis com confirmação de candle. O risco é gerenciado por um stop loss opcional baseado em ATR e trailing stop.

## Detalhes

- **Critérios de entrada**: toque ou rompimento dos níveis Fibonacci 19% / 82,56% com confirmação de candle
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss ou trailing stop
- **Stops**: Sim
- **Valores padrão**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoria: Rompimento Fibonacci
  - Direção: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
