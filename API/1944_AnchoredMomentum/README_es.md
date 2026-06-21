# AnchoredMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AnchoredMomentum calcula la relación entre la EMA y la SMA de los precios de cierre de las velas. Cuando el momentum sube por encima de un umbral superior, abre posiciones largas, y cuando cae por debajo de un umbral inferior, abre posiciones cortas. Las señales opuestas cierran las posiciones actuales.

## Detalles

- **Criterios de entrada**: El momentum cruza por encima de `UpLevel` para ir largo, por debajo de `DownLevel` para ir corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La señal opuesta cierra la posición.
- **Stops**: No.
- **Valores predeterminados**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025m
  - `DownLevel` = -0.025m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
