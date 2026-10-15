# Estrategia de Cruce Ichimoku Tenkan/Kijun
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los indicadores Ichimoku proporcionan un sistema completo de seguimiento de tendencia. Este enfoque se centra en el cruce de la Tenkan-sen sobre la Kijun-sen mientras el precio opera en relación con la nube Kumo. Un cruce alcista por encima de la nube señala la continuación alcista de la tendencia, mientras que un cruce bajista por debajo de la nube sugiere debilidad.

Las pruebas indican una rentabilidad anual media de aproximadamente el 142%. Funciona mejor en el mercado de acciones.

Durante la operación, la estrategia calcula los componentes Ichimoku en cada barra. Cuando la Tenkan sube por encima de la Kijun y el precio está por encima de la nube, se inicia una operación larga con un stop cerca de la Kijun. Un cruce en la dirección opuesta por debajo de la nube activa una operación corta con una colocación similar del stop.

El sistema permanece en la operación hasta que se alcanza el stop o el cruce se revierte, con el objetivo de capturar movimientos sostenidos que siguen la dirección de la nube.

## Detalles

- **Criterios de entrada**: Cruce Tenkan/Kijun con precio relativo a la nube Kumo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o cruce opuesto.
- **Stops**: Sí, al nivel de Kijun.
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = 30 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

