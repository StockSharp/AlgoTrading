# Estrategia Zonal Trading Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Zonal Trading replica el concepto clásico de "zonas" de Bill Williams. Monitorea el color del Awesome Oscillator (AO) y el Accelerator Oscillator (AC). Una barra verde significa que el valor del oscilador aumentó en comparación con la barra anterior, mientras que una barra roja significa que disminuyó. Cuando ambos osciladores se vuelven verdes, la estrategia abre una posición larga. Cuando ambos se vuelven rojos, abre una posición corta. Cualquier color opuesto cierra las posiciones existentes.

## Detalles
- **Criterios de entrada**:
  - **Largo**: AO aumenta y AC aumenta.
  - **Corto**: AO disminuye y AC disminuye.
- **Criterios de salida**:
  - **Largo**: AO o AC disminuye.
  - **Corto**: AO o AC aumenta.
- **Stops**: ninguno por defecto.
- **Parámetros**:
  - `AoCandleType` – marco temporal para el Awesome Oscillator (`H4` por defecto).
  - `AcCandleType` – marco temporal para el Accelerator Oscillator (`H4` por defecto).
  - `BuyOpen`, `SellOpen` – habilitan o deshabilitan las entradas largas y cortas.
  - `BuyClose`, `SellClose` – habilitan o deshabilitan las salidas para posiciones largas y cortas.
- **Indicadores**: Awesome Oscillator (5/34), Accelerator Oscillator (AO menos SMA(5)).
- **Tipo**: seguimiento de momentum, funciona en cualquier mercado y marco temporal donde los osciladores están disponibles.
