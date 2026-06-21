# Estrategia Nova Futures PRO SAFE v6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina señales de tendencia, volatilidad y estructura. Utiliza una EMA de 200 con ADX para confirmar tendencias, Bollinger Bands versus Keltner Channels para detectar rupturas de compresión, y niveles de Donchian para rotura de estructura en máximos o mínimos. Filtros opcionales de marco temporal superior y un índice de irregularidad evitan operar en regímenes de baja calidad. Un período de enfriamiento previene la reentrada inmediata tras el cierre de una posición.

## Entradas
- **EMA Length** — longitud de la media móvil exponencial base
- **DMI Length** — período para ADX y movimiento direccional
- **Min ADX** — valor mínimo de ADX para considerar tendencia
- **BB Length** — período de Bollinger Bands
- **BB Mult** — multiplicador de Bollinger Bands
- **KC Length** — período de Keltner Channels
- **KC Mult** — multiplicador de Keltner Channels
- **Donchian Length** — lookback para niveles de estructura
- **Use HTF** — habilitar confirmación de marco temporal superior
- **HTF Candle** — marco temporal superior para filtros
- **HTF EMA** — longitud de EMA en marco temporal superior
- **HTF Min ADX** — ADX mínimo en marco temporal superior
- **Use Choppiness** — habilitar filtro de irregularidad
- **Chop Length** — período del índice de irregularidad
- **Chop Threshold** — irregularidad máxima permitida
- **Cooldown** — velas a esperar tras una salida
- **Candle Type** — marco temporal principal de velas

## Notas
Port simplificado del script de TradingView "Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown".
