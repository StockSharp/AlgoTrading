# RSI Posição Comprada DAX 2 Horas Dow Jones 1 Hora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

RSI Long Position compra quando o RSI cruza acima do nível de sobrevenda e fecha quando o RSI supera o nível de take profit ou cai abaixo do nível de stop.

## Detalhes

- **Critérios de entrada**: RSI cruza acima de `Oversold`
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: RSI maior que `TakeProfit` ou RSI cruza abaixo de `StopLoss`
- **Stops**: Não
- **Valores padrão**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
