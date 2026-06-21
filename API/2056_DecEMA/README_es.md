# Estrategia DecEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el indicador DecEMA para seguir la dirección de la tendencia. El indicador aplica diez suavizaciones exponenciales consecutivas y las combina para crear una media móvil de bajo retardo. La estrategia compara los últimos tres valores de DecEMA. Si la línea gira hacia arriba y el valor más reciente supera al anterior, compra y cierra cualquier posición corta. Si la línea gira hacia abajo y el valor más reciente está por debajo del anterior, vende y cierra cualquier posición larga.

## Detalles

- **Criterios de entrada**:
  - Largo: la pendiente de DecEMA gira hacia arriba y el valor actual > valor anterior
  - Corto: la pendiente de DecEMA gira hacia abajo y el valor actual < valor anterior
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: la pendiente gira hacia abajo
  - Corto: la pendiente gira hacia arriba
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: DecEMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
