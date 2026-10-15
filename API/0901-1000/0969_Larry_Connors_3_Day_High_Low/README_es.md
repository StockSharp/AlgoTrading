# Estrategia Larry Connors de 3 Días de Máximos y Mínimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el enfoque de reversión a la media de 3 días de máximos/mínimos de Larry Connors.

## Lógica

- Comprar cuando:
  - El cierre está por encima de la media móvil larga.
  - El cierre está por debajo de la media móvil corta.
  - Los máximos y mínimos han sido más bajos durante tres velas consecutivas.
- Salir cuando el precio cierra por encima de la media móvil corta.

## Parámetros

- **Long MA Length** — período para la SMA larga (predeterminado 200)
- **Short MA Length** — período para la SMA corta (predeterminado 5)
- **Candle Type** — marco temporal utilizado para el análisis
