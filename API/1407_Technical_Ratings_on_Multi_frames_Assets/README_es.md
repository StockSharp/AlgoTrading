# Estrategia de Calificaciones Técnicas en Activos Multi-Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia agrega calificaciones técnicas de múltiples marcos temporales.
Compara el precio con una media móvil y los umbrales del RSI en velas de 1h, 4h y diarias.
Se abre una posición larga cuando la calificación combinada es positiva y una posición corta cuando es negativa.

## Detalles

- **Entrada**: Comprar cuando la calificación promedio > 0; vender cuando la calificación promedio < 0.
- **Indicadores**: SMA, RSI.
- **Marcos temporales**: 1h, 4h, 1d.
- **Tipo**: Seguimiento de tendencia.
- **Stops**: Ninguno.
- **Dirección**: Largo y Corto.
- **Riesgo**: Medio.
- **Complejidad**: Medio.
