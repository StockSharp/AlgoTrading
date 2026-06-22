# Estratégia Quantum Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera utilizando o oscilador Stochastic. Quando %K sai da zona de sobrevenda cruzando acima de `LowLevel`, abre uma posição comprada. Quando %K cai da zona de sobrecompra cruzando abaixo de `HighLevel`, abre uma posição vendida. As posições são fechadas em limiares extremos para capturar lucros.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: %K cruza acima de `LowLevel`.
  - **Vendido**: %K cruza abaixo de `HighLevel`.
- **Critérios de saída**:
  - **Comprado**: %K atinge `HighCloseLevel`.
  - **Vendido**: %K atinge `LowCloseLevel`.
- **Indicadores**: Stochastic Oscillator.
- **Período**: Parâmetro `CandleType` (padrão 1 minuto).
- **Parâmetros**:
  - `KPeriod` – período da linha %K.
  - `DPeriod` – período da linha %D.
  - `Slowing` – fator de suavização do Stochastic.
  - `HighLevel` – limite inferior da zona de sobrecompra.
  - `LowLevel` – limite superior da zona de sobrevenda.
  - `HighCloseLevel` – nível para fechar posições compradas.
  - `LowCloseLevel` – nível para fechar posições vendidas.
