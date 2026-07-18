# Estratégia MACD + Bollinger Bands + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta configuração composta busca recuos contra o momentum MACD prevalecente que se estendem além das Bandas de Bollinger. Quando o MACD é positivo mas o preço fecha abaixo da banda inferior com um RSI sobrevendido, a estratégia compra antecipando uma continuação de tendência. O oposto se aplica para vendidos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `MACD > 0` e `Close < LowerBand` e `RSI < 30`
  - **Vendido**: `MACD < 0` e `Close > UpperBand` e `RSI > 70`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, Bollinger Bands, RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
