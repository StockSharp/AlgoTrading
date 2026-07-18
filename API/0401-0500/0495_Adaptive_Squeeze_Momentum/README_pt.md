# Estratégia de Momentum Squeeze Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Momentum Squeeze Adaptativo detecta contrações de volatilidade quando as Bandas de Bollinger caem dentro dos Canais de Keltner e aguarda um rompimento acompanhado de forte momentum. A força do momentum é avaliada por meio de um limiar baseado no desvio padrão. Filtros opcionais de RSI e EMA de tendência refinam as entradas. O ATR pode ser usado para definir níveis dinâmicos de stop-loss e take-profit, e as posições são fechadas após um período de manutenção baseado no tempo.

## Detalhes

- **Critérios de entrada**:
  - O squeeze se libera (Bandas de Bollinger fora dos Canais de Keltner).
  - **Comprado**: Momentum > limiar dinâmico, RSI cruza acima da sobrevenda, EMA de tendência subindo (opcional).
  - **Vendido**: Momentum < -limiar dinâmico, RSI cruza abaixo da sobrecompra, EMA de tendência caindo (opcional).
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto, stop-loss/take-profit baseado em ATR ou saída baseada no tempo.
- **Stops**: Stop-loss e take-profit opcionais baseados em ATR.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Momentum, RSI, EMA, ATR
  - Stops: Opcional
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
