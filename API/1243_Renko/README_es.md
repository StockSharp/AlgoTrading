# Estrategia Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra largo cuando un ladrillo Renko alcista sigue a uno bajista y entra corto en el cambio opuesto.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ladrillo Renko alcista después de un ladrillo bajista.
  - **Corto**: ladrillo Renko bajista después de un ladrillo alcista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa.
- **Stops**: No.
- **Valores predeterminados**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CandleType` = Renko.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Renko
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Renko
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
