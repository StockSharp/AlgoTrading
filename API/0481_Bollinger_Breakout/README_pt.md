# Estratégia de Rompimento Bollinger 4H
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento Bollinger 4H opera rompimentos das Bandas de Bollinger no gráfico de quatro horas. Posições compradas são abertas quando o preço cruza acima da banda inferior com confirmação de volume e tendência. Posições vendidas são abertas quando o preço cruza abaixo da banda superior e o RSI está abaixo de um limiar.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza acima da banda inferior, volume acima de seu SMA e preço acima do SMA de tendência.
  - **Vendido**: O fechamento cruza abaixo da banda superior, volume acima de seu SMA, preço abaixo do SMA de tendência, RSI < 85.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: O fechamento cruza acima da banda superior.
  - **Vendido**: O fechamento cruza abaixo da banda inferior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 1.8
  - `VolumeLength` = 20
  - `TrendLength` = 80
  - `RsiLength` = 14
  - `UseLongSignals` = True
  - `UseShortSignals` = True
- **Filtros**:
  - Categoria: Rompimento de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, SMA de volume, SMA de tendência, RSI
  - Stops: Nenhum
  - Complexidade: Moderado
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
