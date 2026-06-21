# Estrategia VininI Trend LRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia VininI Trend LRMA utiliza una Media Móvil de Regresión Lineal (LRMA) para rastrear la dirección del mercado. La estrategia admite dos modos de entrada:
- **Breakdown**: opera cuando LRMA cruza los niveles superior o inferior fijos.
- **Twist**: opera cuando LRMA invierte su dirección.

## Detalles

- **Criterios de entrada**: LRMA cruza los niveles (Breakdown) o cambia de dirección (Twist)
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: LinearRegression
  - Stops: Ninguno
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
