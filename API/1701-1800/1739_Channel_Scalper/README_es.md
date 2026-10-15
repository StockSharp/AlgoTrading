# Estrategia de Scalper de Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de scalping de ruptura de canal basado en ATR. Para cada vela se calcula el punto medio como el promedio de máximo y mínimo. Las bandas superior e inferior se construyen sumando y restando el Average True Range multiplicado por un factor. Cuando el cierre rompe por encima de la banda superior anterior se abre una posición larga. Una ruptura por debajo de la banda inferior activa una posición corta. Las bandas siguen la dirección de la operación y sirven como stops dinámicos; un cruce de la banda opuesta invierte la posición.

## Detalles

- **Criterios de entrada**:
  - **Compra**: El precio de cierre cruza por encima de la banda superior anterior.
  - **Venta**: El precio de cierre cruza por debajo de la banda inferior anterior.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**:
  - Señal de reversión cuando el precio cruza la banda opuesta.
- **Stops**: Las bandas del canal en trailing actúan como stops.
- **Filtros**: Ninguno.

## Parámetros

- **ATR Period** – número de barras utilizadas para el cálculo del ATR.
- **ATR Multiplier** – factor aplicado al ATR para la distancia de las bandas.
- **Candle Type** – marco temporal de las velas de entrada.
