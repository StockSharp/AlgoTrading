# Bot de Swing Trading Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza níveis de retração Fibonacci para operar movimentos de swing.

Este bot calcula os níveis de retração 0,618 e 0,786 do intervalo das últimas 50 barras e abre posições quando os candles rompem acima ou abaixo desses níveis. O gerenciamento de risco é realizado por meio de parâmetros configuráveis de stop loss e risco/retorno.

## Detalhes

- **Critérios de entrada**: Ação do preço com níveis Fibonacci.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `FiboLevel1` = 0.618
  - `FiboLevel2` = 0.786
  - `RiskRewardRatio` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Swing
  - Direção: Ambos
  - Indicadores: Fibonacci, Donchian
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

