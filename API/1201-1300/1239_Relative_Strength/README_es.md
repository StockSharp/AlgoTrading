# Estrategia de Fuerza Relativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula una medida de fuerza relativa ponderada a partir de múltiples medias móviles.
Las bandas de Bollinger sobre la señal de fuerza indican zonas de sobrecompra y sobreventa.
La estrategia compra cuando la fuerza sube por encima de la banda superior y vende cuando cae por debajo de la banda inferior.

## Detalles

- **Entrada**: la fuerza cruza por encima de la banda superior para largo, por debajo de la banda inferior para corto.
- **Salida**: cruce de banda opuesto.
- **Indicadores**: EMA 8, EMA 34, SMA 20, SMA 50, SMA 200, Bollinger Bands.
- **Tipo**: Momentum.
