# Estratégia de Política Energética Avançada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Energy Advanced Policy** combina sentimento de política com filtros técnicos básicos.

- **Comprado**: EMA(21) acima da EMA(55), RSI abaixo de sobrecomprado, Bandas de Bollinger sem compressão.
- **Saída**: RSI cruza acima de sobrecomprado ou a tendência EMA se reverte.

## Parâmetros
- `NewsSentiment` – sentimento manual.
- `EnableNewsFilter` – habilitar substituição de sentimento de política.
- `EnablePolicyDetection` – permitir detecção de eventos de política.
- `PolicyVolumeThreshold` – múltiplo de pico de volume.
- `PolicyPriceThreshold` – limiar de variação de preço (%).
- `RsiLength` – período do RSI.
- `RsiOverbought` – nível de sobrecomprado do RSI.
- `FastLength` – período da EMA rápida.
- `SlowLength` – período da EMA lenta.
- `BbLength` / `BbMult` – configurações das Bandas de Bollinger.

Indicadores: RSI, EMA, Bollinger Bands.
