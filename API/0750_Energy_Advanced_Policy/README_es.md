# Estrategia de Política Energética Avanzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Energy Advanced Policy** combina el sentimiento de política con filtros técnicos básicos.

- **Largo**: EMA(21) por encima de EMA(55), RSI por debajo de sobrecompra, Bandas de Bollinger sin compresión.
- **Salida**: RSI cruza por encima de sobrecompra o la tendencia EMA se revierte.

## Parámetros
- `NewsSentiment` – sentimiento manual.
- `EnableNewsFilter` – habilitar filtro de sentimiento de política.
- `EnablePolicyDetection` – permitir detección de eventos de política.
- `PolicyVolumeThreshold` – múltiplo de pico de volumen.
- `PolicyPriceThreshold` – umbral de cambio de precio (%).
- `RsiLength` – período del RSI.
- `RsiOverbought` – nivel de sobrecompra del RSI.
- `FastLength` – período de la EMA rápida.
- `SlowLength` – período de la EMA lenta.
- `BbLength` / `BbMult` – configuración de las Bandas de Bollinger.

Indicadores: RSI, EMA, Bollinger Bands.
