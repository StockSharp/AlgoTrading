# Estratégia de Trading Fibonacci Contra-Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza uma Média Móvel Ponderada por Volume (VWMA) e o desvio padrão para construir bandas de Fibonacci. Entra comprado quando o preço cai abaixo da banda inferior selecionada e vendido quando o preço sobe acima da banda superior. Opcionalmente, as posições são fechadas quando o preço cruza a base VWMA.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza abaixo da banda inferior escolhida.
  - **Vendido**: O fechamento cruza acima da banda superior escolhida.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Base**: Saída opcional quando o preço cruza a VWMA.
  - **Reversão**: O sinal da banda oposta reverte a posição.
- **Stops**: Nenhum.
- **Indicadores**: VolumeWeightedMovingAverage, StandardDeviation.
