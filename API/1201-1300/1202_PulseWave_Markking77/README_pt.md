# Estratégia PulseWave
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza VWAP, cruzamento de MACD e filtro RSI.

A estratégia compra quando o preço está acima do VWAP, o MACD cruza acima de sua linha de sinal e o RSI está abaixo do limiar de sobrecompra. Sai quando o preço cai abaixo do VWAP, o MACD cruza abaixo da linha de sinal e o RSI está acima do limiar de sobrevenda.

## Detalhes

- **Critérios de entrada**: Preço acima do VWAP, MACD cruzando para cima, RSI abaixo de sobrecompra.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Preço abaixo do VWAP, MACD cruzando para baixo, RSI acima de sobrevenda.
- **Stops**: Não.
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: VWAP, MACD, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
