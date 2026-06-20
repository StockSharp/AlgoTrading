# Ichimoku Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores Ichimoku Cloud y ADX. Criterios de entrada:
Largo: Price > Kumo (nube) && Tenkan > Kijun && ADX > 25 (tendencia alcista con movimiento fuerte) Corto: Price < Kumo (nube) && Tenkan < Kijun && ADX > 25 (tendencia bajista con movimiento fuerte) Criterios de salida: Largo: Price < Kumo (precio cae por debajo de la nube) Corto: Price > Kumo (precio sube por encima de la nube)

Las pruebas indican un rendimiento anual promedio de aproximadamente 187%. Funciona mejor en el mercado de acciones.

Esta estrategia combina señales de Ichimoku Cloud con ADX para filtrar tendencias poderosas. Las operaciones ocurren cuando el precio rompe por encima o por debajo de la nube con confirmación del ADX.

Favorece a los operadores que prefieren configuraciones de tendencia estructuradas. Los stops definidos por ATR defienden contra oscilaciones adversas.

## Detalles

- **Criterios de entrada**:
  - Largo: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - Corto: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio cruza la nube en dirección opuesta
- **Stops**: Usa la nube Ichimoku como stop trailing
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku Cloud, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

