# Estratégia de Índice de Força Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base em um Force Index suavizado que cruza níveis definidos pelo usuário. Quando o indicador sobe acima do nível alto ou cai abaixo do nível baixo, a estratégia abre ou fecha posições dependendo do modo de operação selecionado. O Force Index é calculado a partir da variação de preço e do volume e suavizado com uma EMA.

## Detalhes

- **Critérios de entrada**
  - *Modo direto*:
    - **Comprado**: o indicador cruza acima de `HighLevel`.
    - **Vendido**: o indicador cruza abaixo de `LowLevel`.
  - *Modo contra tendência*:
    - **Comprado**: o indicador cruza abaixo de `LowLevel`.
    - **Vendido**: o indicador cruza acima de `HighLevel`.
- **Critérios de saída**
  - *Modo direto*:
    - **Comprado**: cruzamento abaixo de `LowLevel`.
    - **Vendido**: cruzamento acima de `HighLevel`.
  - *Modo contra tendência*:
    - **Comprado**: cruzamento acima de `HighLevel`.
    - **Vendido**: cruzamento abaixo de `LowLevel`.
- **Stops**: Não.
- **Valores padrão**:
  - `Period` = 30
  - `HighLevel` = 0
  - `LowLevel` = 0
  - `Candle Type` = 4-hour
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Force Index
  - Stops: Não
  - Complexidade: Médio
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
