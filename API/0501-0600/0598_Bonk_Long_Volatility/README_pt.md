# BONK Volatilidade Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia somente comprado entra em condições de alta fortes combinando médias móveis, volatilidade e filtros de volume. Compra quando o mercado está em tendência de alta, a volatilidade se expande e indicadores de momentum confirmam a força. As saídas usam take profit fixo, stop loss e um trailing stop baseado em ATR.

## Detalhes

- **Critérios de entrada**: MA rápida acima da MA lenta, intervalo de preço maior que ATR * `AtrMultiplier`, RSI entre `RsiOversold` e `RsiOverbought`, linha MACD acima do sinal e zero, volume acima de SMA * `VolumeThreshold`, fechamento acima da MA rápida, vela dentro dos últimos `LookbackDays`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Take profit, stop loss ou trailing stop baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: SMA, ATR, RSI, MACD, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

