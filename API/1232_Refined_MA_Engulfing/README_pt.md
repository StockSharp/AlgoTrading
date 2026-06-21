# MA Refinada + Engolfamento (M5 + Quebra de Estrutura Confirmada)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

MA Refinada + Engolfamento combina duas médias móveis simples, candles de engolfamento e confirmação de quebra de estrutura. Uma operação é colocada quando pelo menos dois fatores de confluência se alinham e o período de resfriamento passou.

## Detalhes

- **Critérios de entrada**: Após uma quebra de estrutura de alta ou baixa confirmada, preço acima ou abaixo de ambas as SMAs, e pelo menos duas de quatro confluências (engolfamento, quebra de estrutura, filtro MA, marcador fib) com o resfriamento satisfeito.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Nenhum.
- **Stops**: Não.
- **Valores padrão**:
  - `Ma1Length` = 66
  - `Ma2Length` = 85
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: SMA, Engulfing, Structure Break
  - Stops: Não
  - Complexidade: Intermediário
  - Período: 5-minute
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
