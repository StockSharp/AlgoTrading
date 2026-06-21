# IMACD Sniper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

IMACD Sniper combina cruzamentos do MACD com um filtro de tendência EMA, confirmação de volume e padrões de velas fortes. O take profit e o stop loss dinâmicos são baseados no intervalo médio recente.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A linha MACD cruza acima da linha de sinal, preço acima da EMA, delta do MACD > delta mínimo, ambas as linhas longe de zero, volume acima da média, vela altista forte.
  - **Vendido**: A linha MACD cruza abaixo da linha de sinal, preço abaixo da EMA, delta do MACD > delta mínimo, ambas as linhas longe de zero, volume acima da média, vela baixista forte.
- **Critérios de saída**: Cruzamento oposto do MACD ou atingir take profit / stop loss.
- **Stops**: Take profit e stop loss dinâmicos baseados no intervalo médio.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado e Vendido
  - Indicadores: MACD, EMA, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
