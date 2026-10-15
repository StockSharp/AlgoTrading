# Estrategia de Zonas de Trading AO AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el concepto "AO/AC Trading Zones". Combina el Awesome Oscillator (AO), Acceleration/Deceleration (AC) y los fractales de Bill Williams para construir una pirámide de posiciones largas cuando el momentum acelera por encima de la línea de dientes del Alligator.

## Detalles

- **Entrada**: Al menos dos barras consecutivas con `close > teeth`, `AO > AO[1]`, `AC > AC[1]`, y `close > EMA`.
- **Piramidación**: Añade hasta cinco posiciones largas mientras las condiciones sean válidas.
- **Salida**: Reversión de tendencia por fractales o precio cayendo por debajo del nivel de stop.
- **Indicadores**: SMMA (dientes), AO, AC, EMA.
- **Stop**: Mínimo de la quinta barra verde.
