# Ajuste Fino de Entradas: Análisis Híbrido de Dispersión de Volumen Suavizado por Fourier
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el volumen suavizado con la EMA de los precios de apertura y cierre para analizar la dispersión de volumen. Entra en largo cuando tanto la dispersión de volumen como su media móvil son positivas, y en corto cuando ambas son negativas. Un parámetro opcional permite cerrar posiciones cuando no hay señal.

## Detalles

- **Condiciones de entrada**:
  - **Largo**: `vd > 0` y `vdma > 0`
  - **Corto**: `vd < 0` y `vdma < 0`
- **Condiciones de salida**: Opcionalmente cerrar posición cuando las señales son neutrales.
- **Tipo**: Seguimiento de tendencia
- **Indicadores**: EMA
- **Marco temporal**: 1 minuto (predeterminado)
