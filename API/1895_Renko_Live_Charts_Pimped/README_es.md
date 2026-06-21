# Estrategia Renko Live Charts Pimped
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye ladrillos Renko y opera en los cambios de dirección. Opcionalmente puede calcular el tamaño del ladrillo a partir de valores ATR, lo que permite que la estructura Renko se adapte a la volatilidad del mercado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ladrillo Renko alcista después de uno bajista.
  - **Corto**: ladrillo Renko bajista después de uno alcista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa.
- **Stops**: No.
- **Valores predeterminados**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Renko, ATR
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Renko
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
