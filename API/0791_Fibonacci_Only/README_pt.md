# Estratégia Exclusiva Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Exclusiva Fibonacci utiliza níveis de retração personalizados de 19% e 82,56% derivados dos últimos 100 candles. A estratégia entra quando o preço toca ou rompe esses níveis com confirmação da direção do candle. Suporta entradas opcionais por rompimento, stop loss baseado em ATR, trailing stop e sete alvos de take profit escalonados.

## Detalhes

- **Critérios de entrada**: toque ou rompimento dos níveis Fibonacci com confirmação
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss, trailing stop ou alvos de take profit
- **Stops**: ATR ou percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoria: Fibonacci
  - Direção: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
