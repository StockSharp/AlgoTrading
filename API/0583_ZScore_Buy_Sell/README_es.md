# Estrategia de Compra/Venta por ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el Z-Score para detectar desviaciones extremas respecto a la media móvil.
Se abre una posición cuando el Z-Score cruza por encima o por debajo de un umbral, y un período de enfriamiento evita señales repetidas.

## Detalles

- **Criterios de entrada**:
  - Corto cuando el Z-Score > `ZThreshold` y el enfriamiento de venta ha pasado.
  - Largo cuando el Z-Score < -`ZThreshold` y el enfriamiento de compra ha pasado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation, Z-Score
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
