# Estratégia RSI Stochastic WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina RSI, Oscilador Estocástico e uma Média Móvel Ponderada (WMA).
Compra quando o RSI está sobrevendido, %K cruza acima de %D e o preço está acima da WMA.
Vende a descoberto quando o RSI está sobrecomprado, %K cruza abaixo de %D e o preço está abaixo da WMA.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI < 30 && %K crosses above %D && Close > WMA`
  - Vendido: `RSI > 70 && %K crosses below %D && Close < WMA`
- **Comprado/Vendido**: Ambos
- **Stops**: Nenhum
- **Valores padrão**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RSI, Stochastic, WMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
