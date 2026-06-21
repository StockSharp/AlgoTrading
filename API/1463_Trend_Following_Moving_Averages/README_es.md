# Estrategia de Seguimiento de Tendencia con Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Calcula una media móvil y mide su tendencia dentro de un canal de precios dinámico.
Se toman posiciones largas cuando la puntuación de tendencia es positiva y posiciones cortas cuando es negativa.

## Detalles

- **Entrada**:
  - **Largo**: puntuación de tendencia > 0
  - **Corto**: puntuación de tendencia < 0
- **Salida**: señal inversa
- **Indicadores**: SMA, Highest, Lowest
- **Marco temporal**: configurable
- **Tipo**: Seguimiento de tendencia
