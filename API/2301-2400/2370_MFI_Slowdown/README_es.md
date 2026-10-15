# Desaceleración MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia monitorea el Índice de Flujo de Dinero (MFI) en un marco temporal superior y reacciona cuando alcanza zonas extremas. Si `SeekSlowdown` está habilitado, una señal se confirma solo cuando el valor del MFI cambia menos de un punto entre dos barras consecutivas. En una señal ascendente cierra posiciones cortas y opcionalmente abre una nueva posición larga; en una señal descendente cierra posiciones largas y puede abrir una corta. La gestión del riesgo se maneja mediante StartProtection.

## Detalles

- **Criterios de entrada**:
  - Señal ascendente: `MFI >= UpperThreshold` y (sin verificación de desaceleración o desaceleración detectada).
  - Señal descendente: `MFI <= LowerThreshold` y (sin verificación de desaceleración o desaceleración detectada).
- **Largo/Corto**: Ambos, dependiendo de los parámetros.
- **Criterios de salida**:
  - La señal opuesta cierra la posición.
  - Stop-loss y take-profit mediante `StopLossPercent` y `TakeProfitPercent`.
- **Stops**: Sí, mediante StartProtection.
- **Valores predeterminados**:
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = marco temporal de 6 horas
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MFI
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Opcional (verificación de desaceleración)
  - Nivel de riesgo: Medio
