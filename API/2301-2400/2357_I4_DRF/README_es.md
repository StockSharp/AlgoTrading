# Estrategia I4 DRF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador personalizado I4 DRF. Compara la dirección de los máximos y mínimos recientes de las velas y genera un valor entre -100 y +100. Las acciones de trading dependen de las transiciones de color de este indicador y del modo seleccionado.

## Detalles

- **Criterios de entrada**:
  - Modo `Direct`: abrir largo cuando el indicador cambia de positivo a negativo, abrir corto cuando cambia de negativo a positivo.
  - Modo `NotDirect`: abrir largo en un cambio de negativo a positivo, abrir corto en un cambio de positivo a negativo.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Las posiciones se cierran cuando aparece la señal opuesta.
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Period` = 11
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: I4 DRF
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
