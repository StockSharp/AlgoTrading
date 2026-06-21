# Estratégia Multi-Fator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Multi-Fator combina MACD, RSI e duas médias móveis para operar com confirmação de tendência. Operações compradas ocorrem quando a linha MACD está acima do seu sinal, o RSI está abaixo de 70, o preço está acima da SMA de 50 períodos e a SMA de 50 está acima da SMA de 200. Operações vendidas usam condições opostas.

Stops e alvos são baseados em múltiplos do ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **Vendido**: `MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss e take profit baseados em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, RSI, SMA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
