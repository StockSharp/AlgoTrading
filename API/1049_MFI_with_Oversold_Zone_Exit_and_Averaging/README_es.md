# Estrategia MFI con Salida de Zona de Sobreventa y Promediación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia espera a que el Money Flow Index (MFI) entre en la zona de sobreventa. Una vez que el MFI sube por encima del nivel de sobreventa, coloca una orden de compra limitada a un porcentaje fijo por debajo del cierre actual. Si la orden no se ejecuta dentro de un número especificado de barras, se cancela. El stop-loss y el take-profit se aplican mediante StartProtection.

## Detalles

- **Criterios de entrada**:
  - El MFI sube por encima de `MfiOversoldLevel` tras haber estado por debajo; se coloca una compra limitada `LongEntryPercentage` por debajo del cierre.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**:
  - Posición cerrada por take-profit o stop-loss (`ExitGainPercentage`, `StopLossPercentage`).
- **Stops**: Sí, mediante StartProtection.
- **Valores predeterminados**:
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: MFI
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
