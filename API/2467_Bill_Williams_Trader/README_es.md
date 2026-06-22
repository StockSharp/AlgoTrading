# Estrategia Bill Williams Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa una versión simplificada del enfoque de trading de Bill Williams basado en el indicador **Alligator** y los **Fractals**.

## Cómo funciona

- Calcula las líneas del Alligator usando Medias Móviles Suavizadas (SMMA):
  - Longitud de **Jaw** (predeterminado 13)
  - Longitud de **Teeth** (predeterminado 8)
  - Longitud de **Lips** (predeterminado 5)
- Detecta fractales alcistas y bajistas en velas completadas.
- **Comprar** cuando el precio rompe por encima del último fractal superior que está por encima de la línea teeth del Alligator.
- **Vender** cuando el precio rompe por debajo del último fractal inferior que está por debajo de la línea teeth del Alligator.
- **Salir** de posiciones largas cuando el precio de cierre cae por debajo de la línea lips.
- **Salir** de posiciones cortas cuando el precio de cierre sube por encima de la línea lips.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `JawLength` | Período de la SMMA de la mandíbula del Alligator | 13 |
| `TeethLength` | Período de la SMMA de los dientes del Alligator | 8 |
| `LipsLength` | Período de la SMMA de los labios del Alligator | 5 |
| `CandleType` | Tipo de vela usado para los cálculos | Velas de 15 minutos |

Todos los parámetros pueden optimizarse mediante la interfaz de parámetros de la estrategia.

## Uso

1. Compilar la solución:
   ```bash
   dotnet build
   ```
2. Lanzar la estrategia dentro del entorno StockSharp y seleccionar el instrumento y el marco temporal deseados.

## Notas

Este ejemplo demuestra el uso de la API de alto nivel con vinculaciones de indicadores y no implementa dimensionamiento de posiciones ni gestión de riesgos más allá de salidas simples.
