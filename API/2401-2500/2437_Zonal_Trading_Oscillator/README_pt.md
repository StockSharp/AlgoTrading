# Estratégia Zonal Trading Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Zonal Trading replica o conceito clássico de "zonas" de Bill Williams. Monitoriza a cor do Awesome Oscillator (AO) e do Accelerator Oscillator (AC). Uma barra verde significa que o valor do oscilador aumentou em relação à barra anterior, enquanto uma barra vermelha significa que diminuiu. Quando ambos os osciladores ficam verdes, a estratégia abre uma posição comprada. Quando ambos ficam vermelhos, abre uma posição vendida. Qualquer cor oposta fecha as posições existentes.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: AO aumenta e AC aumenta.
  - **Vendido**: AO diminui e AC diminui.
- **Critérios de saída**:
  - **Comprado**: AO ou AC diminui.
  - **Vendido**: AO ou AC aumenta.
- **Stops**: nenhum por padrão.
- **Parâmetros**:
  - `AoCandleType` – período temporal para o Awesome Oscillator (`H4` por padrão).
  - `AcCandleType` – período temporal para o Accelerator Oscillator (`H4` por padrão).
  - `BuyOpen`, `SellOpen` – ativam ou desativam entradas compradas e vendidas.
  - `BuyClose`, `SellClose` – ativam ou desativam saídas para posições compradas e vendidas.
- **Indicadores**: Awesome Oscillator (5/34), Accelerator Oscillator (AO menos SMA(5)).
- **Tipo**: seguimento de momentum, funciona em qualquer mercado e período onde os osciladores estão disponíveis.
