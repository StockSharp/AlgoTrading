# Estrategia Waindrops Makit0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simplificada que compara el VWAP de dos mitades de un período personalizado.

## Detalles

- **Criterios de entrada**: VWAP de la mitad derecha frente al VWAP de la mitad izquierda.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: VWAP
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
