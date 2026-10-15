# Estratégia de Bandas Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Expande um Canal Keltner por razões Fibonacci e opera quando o preço rompe a banda externa com confirmação do RSI.

## Detalhes

- **Critérios de entrada**: Preço cruza `fbUpper3` com RSI acima de 60 para comprado; cruza `fbLower3` com RSI abaixo de 40 para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruza de volta sobre a média móvel.
- **Stops**: Não.
- **Valores padrão**:
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: MA, ATR, RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
