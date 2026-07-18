# Estratégia de Ciclo de Tendência de Momentum Color Schaff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o Color Schaff Momentum Trend Cycle (STC) para detectar reversões de tendência quando o indicador sai das zonas de sobrecompra ou sobrevenda.

## Detalhes

- **Critérios de entrada**:
  - Comprar quando a cor STC anterior estava acima da zona superior (>5) e a cor atual cai abaixo de 6, fechando quaisquer posições vendidas.
  - Vender quando a cor STC anterior estava abaixo da zona inferior (<2) e a cor atual sobe acima de 1, fechando quaisquer posições compradas.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O sinal inverso fecha a posição oposta.
- **Stops**: Sem stop loss ou take profit explícito.
- **Valores padrão**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

