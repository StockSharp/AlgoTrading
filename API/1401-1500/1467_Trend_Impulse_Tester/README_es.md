# Estrategia de Probador de Impulso de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend Impulse Tester entra en operaciones cuando una tendencia fuerte es confirmada por EMAs y ADX y aparece un impulso RSI.
Compra en impulsos alcistas durante tendencias alcistas y vende en impulsos bajistas durante tendencias bajistas.

## Detalles

- **Criterios de entrada**: tendencia EMA + confirmación ADX con RSI cruzando el umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ADX, RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
