# Estratégia de Toque de Bollinger Band com Ângulos SMI e MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando o preço toca a banda inferior de Bollinger e tanto os ângulos SMI quanto MACD apontam para cima. A posição é fechada quando o preço atinge a banda superior de Bollinger.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento toca ou cai abaixo da banda inferior de Bollinger e os ângulos SMI/MACD são positivos mas abaixo de seus limites.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: O preço de fechamento toca ou excede a banda superior de Bollinger.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: Bollinger Bands, Stochastic (SMI), MACD
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: 1H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
