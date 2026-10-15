# Estratégia EMA & CDC Trailing Stop Melhorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina filtro de tendência EMA, confirmação do MACD e um stop trailing CDC baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço > EMA60, EMA60 > EMA90, linha MACD > linha de sinal.
  - **Vendido**: preço < EMA60, EMA60 < EMA90, linha MACD < linha de sinal.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Trailing stop ou alvo de lucro baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, MACD, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
