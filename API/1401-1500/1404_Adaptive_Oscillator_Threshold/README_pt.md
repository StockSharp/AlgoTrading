# Estratégia de Limiar de Oscilador Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Limiar de Oscilador Adaptativo usa o RSI com um limiar dinâmico baseado no Limiar Adaptativo de Bufi (BAT). Compra quando o RSI cai abaixo de um nível fixo ou de um limiar adaptativo.

## Detalhes

- **Critérios de entrada**: RSI cai abaixo do limiar fixo ou adaptativo
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: Saída por barras fixas ou stop-loss em dólares
- **Stops**: Stop-loss em dólares
- **Valores padrão**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: RSI, StandardDeviation, LinearRegression
  - Stops: Dólar
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
