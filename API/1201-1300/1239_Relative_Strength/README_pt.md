# Estratégia de Força Relativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula uma medida de força relativa ponderada a partir de múltiplas médias móveis.
As Bollinger Bands sobre o sinal de força indicam zonas de sobrecompra e sobrevenda.
A estratégia compra quando a força sobe acima da banda superior e vende quando cai abaixo da banda inferior.

## Detalhes

- **Entrada**: a força cruza acima da banda superior para comprado, abaixo da banda inferior para vendido.
- **Saída**: cruzamento da banda oposta.
- **Indicadores**: EMA 8, EMA 34, SMA 20, SMA 50, SMA 200, Bollinger Bands.
- **Tipo**: Momentum.
