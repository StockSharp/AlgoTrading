# Estrategia Tres Rojas / Tres Verdes con Filtro ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo después de tres velas bajistas consecutivas si el ATR está por encima de su SMA de 30 períodos. Sale después de tres velas alcistas o cuando se alcanza la duración máxima de la operación.

## Parámetros

- **CandleType**: Tipo de velas.
- **MaxTradeDuration**: Número máximo de barras para mantener una posición abierta.
- **UseGreenExit**: Si se debe salir después de tres velas verdes.
- **AtrPeriod**: Período para el cálculo del ATR (0 desactiva el filtro).
