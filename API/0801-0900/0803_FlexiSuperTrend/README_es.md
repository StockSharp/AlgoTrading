# Estrategia FlexiSuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina un filtro Supertrend con un oscilador de desviación suavizado.
Se abre una posición cuando el precio coincide con la dirección del Supertrend y el
oscilador confirma el momentum.

## Detalles

- **Criterios de entrada**:
  - Precio por encima del Supertrend y desviación (SMA del precio menos Supertrend) > 0 → compra.
  - Precio por debajo del Supertrend y desviación < 0 → venta.
- **Largo/Corto**: Ambas direcciones pueden habilitarse.
- **Criterios de salida**:
  - Reversión de tendencia cuando el precio cruza la línea del Supertrend.
- **Stops**: Sin lógica de stop por defecto.
- **Valores predeterminados**:
  - Período ATR = 10.
  - Factor ATR = 3.0.
  - Longitud SMA = 10.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, SMA
  - Stops: Ninguno
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
