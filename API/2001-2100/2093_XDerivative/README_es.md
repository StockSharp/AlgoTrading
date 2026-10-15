# Estrategia XDerivative
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia XDerivative rastrea cambios en el momentum del precio usando una tasa de cambio suavizada. El experto MQL original combina un cálculo de tasa de cambio con suavizado Jurik para detectar puntos de giro. La versión de StockSharp reutiliza indicadores integrados para implementar el mismo concepto.

La estrategia calcula la tasa de cambio sobre `RocPeriod` barras y la suaviza con una Jurik Moving Average de longitud `MaLength`. Cuando la derivada suavizada forma un valle (el valor anterior es inferior a su predecesor y el valor actual sube por encima del anterior), la estrategia entra o cambia a una posición larga. Cuando se forma un pico (el valor anterior es superior a su predecesor y el valor actual cae por debajo de él), la estrategia entra o cambia a una posición corta. Los stops de protección gestionan las salidas.

## Detalles

- **Criterios de entrada**:
  - Largo: La derivada suavizada gira hacia arriba después de un mínimo local.
  - Corto: La derivada suavizada gira hacia abajo después de un máximo local.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Giro opuesto de la derivada o stop de protección.
- **Stops**: Sí, take profit y stop loss en porcentaje.
- **Valores predeterminados**:
  - `RocPeriod` = 34
  - `MaLength` = 7
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RateOfChange, JurikMovingAverage
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
