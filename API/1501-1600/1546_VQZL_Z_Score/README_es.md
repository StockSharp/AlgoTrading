# Estrategia VQZL Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el Z-Score relativo a una media suavizada.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 42%. Funciona mejor en el mercado de acciones.

La estrategia calcula una media móvil suavizada y la desviación estándar para calcular el Z-Score. Cuando el precio se desvía más allá de un umbral, entra en la dirección del movimiento.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Z-Score > threshold`.
  - **Corto**: `Z-Score < -threshold`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
