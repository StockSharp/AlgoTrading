# Estrategia de Compra en Nube Ichimoku con Salida EMA Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia de compra en la Nube Ichimoku con salida EMA personalizada y filtro de volumen. La estrategia compra cuando el precio está por encima de la nube y el volumen supera su promedio. Opcionalmente requiere que el precio se mantenga por encima de la EMA. La posición se cierra cuando el precio cae por debajo de la EMA o cuando se activa el stop-loss.

## Detalles

- **Criterios de entrada**:
  - Largo: `Price > Cloud && Volume > AvgVolume && (Price > EMA if enabled)`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - `Price < EMA`
- **Stops**: Basado en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `EmaLength` = 44
  - `VolumeAvgPeriod` = 10
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Nube Ichimoku, EMA, Volumen
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
