# Estrategia de Stop Porcentual y Toma de Ganancias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos medias móviles simples (SMA) para detectar la dirección de la tendencia. Cuando la SMA rápida cruza por encima de la SMA lenta, abre una posición larga. Cuando la SMA rápida cruza por debajo de la SMA lenta, abre una posición corta. Tras la entrada, la estrategia establece niveles de stop-loss y toma de ganancias como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La SMA rápida cruza por encima de la SMA lenta.
  - **Corto**: La SMA rápida cruza por debajo de la SMA lenta.
- **Criterios de salida**:
  - Stop-loss y toma de ganancias basados en porcentajes del precio de entrada.
- **Stops**: Sí, tanto stop-loss como toma de ganancias.
- **Indicadores**: SMA.
- **Categoría**: Seguimiento de tendencia.
- **Marco temporal**: Cualquiera.
