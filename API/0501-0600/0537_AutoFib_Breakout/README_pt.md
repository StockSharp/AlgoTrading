# Estratégia de Rompimento AutoFib
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia traça uma extensão de Fibonacci dinâmica a partir do recente máximo e mínimo de swing e vai comprado quando o preço rompe acima do nível 1.618 durante uma tendência de alta definida pela EMA de 200 períodos. O risco é gerenciado por meio de stop e alvo baseados em ATR.

## Detalhes

- **Critérios de entrada**: Fechamento acima da extensão 1.618 de Fibonacci e acima da EMA200.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss baseado em ATR ou take profit de 3×ATR.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `FibLevel` = 1.618
  - `PivotPeriod` = 10
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: EMA, ATR, Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
