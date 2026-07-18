# Estrategia de Ajuste Fino de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea la pendiente de una media móvil simple. Después de dos barras consecutivas en una dirección, un cambio de dirección de la media móvil dispara una entrada. Un giro al alza después de una bajada abre una posición larga, mientras que un giro a la baja después de un alza abre una posición corta. Las señales opuestas cierran las operaciones existentes.

El sistema fue convertido del asesor MQL "Exp_FineTuningMA" y reemplaza el indicador personalizado original con una media móvil simple estándar para mayor claridad.

## Detalles

- **Criterios de entrada**: La MA cambia de dirección después de dos barras.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `MaLength` = 10
  - `TakeProfitPercent` = 1
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Swing / H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
