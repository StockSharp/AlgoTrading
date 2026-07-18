# Estrategia FlexiMA de Seguimiento de Varianza
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rastrea la desviación del precio respecto a una media móvil y abre operaciones cuando la desviación supera un umbral de volatilidad mientras la dirección del Supertrend lo confirma.

## Detalles

- **Criterios de entrada**:
  - Precio por encima del Supertrend y desviación > promedio + desviación estándar × multiplicador → compra.
  - Precio por debajo del Supertrend y desviación < -(promedio + desviación estándar × multiplicador) → venta.
- **Largo/Corto**: Ambas direcciones pueden habilitarse.
- **Criterios de salida**:
  - Desviación opuesta o reversión del Supertrend.
- **Stops**: Sin lógica de stop por defecto.
- **Valores predeterminados**:
  - Longitud de MA = 20.
  - Longitud de StdDev = 20.
  - Multiplicador StdDev = 1.0.
  - Período ATR = 10.
  - Factor ATR = 3.0.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation, SuperTrend
  - Stops: Ninguno
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
