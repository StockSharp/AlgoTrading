# Estrategia de Analizador de Órdenes de Bloque de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simplificada basada en el script de TradingView **"Volume Block Order Analyzer"**. Mide cómo los grandes picos de volumen impactan la dirección del precio y acumula este efecto a lo largo del tiempo. Cuando el impacto acumulado supera los umbrales definidos por el usuario, la estrategia entra en operaciones y las protege con un stop trailing.

## Detalles

- **Entrada**: Impacto acumulado por encima o por debajo del umbral.
- **Salida**: Stop trailing basado en un porcentaje desde el punto de entrada.
- **Largo/Corto**: Ambos.
- **Indicadores**: SMA.
- **Marco temporal**: Cualquiera.

Este puerto se centra en la idea principal; muchas características visuales del script original están omitidas.
