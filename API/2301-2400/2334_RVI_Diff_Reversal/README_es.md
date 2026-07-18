# Estrategia de Reversión de Diferencia RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera basándose en la diferencia suavizada entre el Relative Vigor Index (RVI) y su línea de señal.
Detecta los puntos donde esta diferencia deja de caer y comienza a subir para entrar en largo, y viceversa para posiciones cortas.

## Detalles

- **Criterios de entrada**: Reversión de pendiente de la diferencia RVI suavizada
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `RviLength` = 12
  - `SmoothingLength` = 13
  - `CandleType` = velas de 6 horas
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RVI, SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 6H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
