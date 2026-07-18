# Estrategia de Zona Wlx BW5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el Awesome Oscillator (AO) y el Accelerator Oscillator (AC) de Bill Williams para identificar secuencias de momentum fuertes. Una señal de compra (venta) aparece cuando ambos osciladores suben (bajan) durante cinco barras consecutivas. El sistema revierte o abre posiciones en consecuencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `AO` y `AC` subiendo durante cinco barras consecutivas.
  - **Corto**: `AO` y `AC` bajando durante cinco barras consecutivas.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Revertir posición ante señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Timeframe` = 4 horas.
  - `Direct` = true.
  - `SignalBar` = 1.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
