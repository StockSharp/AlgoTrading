# Información de Ciclo de Amplitud HL de Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera basándose en la amplitud del canal Donchian y los cambios de ciclo.

La estrategia monitorea la relación de los extremos de las velas con el canal Donchian. Tras un ciclo bajista, tocar la banda superior abre una posición larga. Tras un ciclo alcista, tocar la banda inferior abre una posición corta.

## Detalles

- **Criterios de entrada**: Cambio de tendencia de ciclo en el canal Donchian.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal de ciclo opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 28
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Donchian
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
