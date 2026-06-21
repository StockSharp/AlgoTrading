# Estrategia de Reversión XMA de 3ª Generación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Emplea una media móvil exponencial de doble suavizado conocida como XMA de 3ª Generación para detectar máximos y mínimos locales. Se abre una posición larga cuando el XMA gira al alza desde un mínimo local. Los cortos se inician cuando el XMA se revierte desde un máximo local. Las posiciones se invierten ante señales opuestas y no se utiliza stop ni take profit explícito.

## Detalles
- **Criterios de entrada**: El XMA forma un mínimo o máximo local y se revierte.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MaLength` = 50
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (4H)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
