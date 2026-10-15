# Estrategia Plantilla de Orden Stop ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia demuestra cómo colocar órdenes stop pendientes usando el Average Directional Index (ADX) y sus componentes de Movimiento Direccional. Recrea la lógica central de una plantilla MQL clásica: cuando el mercado muestra una tendencia fuerte y las líneas +DI y -DI se cruzan, el sistema coloca una orden de compra stop o venta stop a una distancia fija. Los niveles de stop-loss y take-profit de protección se gestionan automáticamente.

El ejemplo es intencionalmente simple y se enfoca en el manejo de órdenes. Los traders pueden extenderlo con filtros adicionales o reglas de gestión del dinero para construir sistemas más avanzados.

## Detalles

- **Criterios de entrada**:
  - Valor ADX por encima del parámetro `ADX Threshold`.
  - **Largo**: `+DI` mayor que `-DI` y hace dos velas `+DI` estaba por debajo de `-DI`.
  - **Corto**: `+DI` menor que `-DI` y hace dos velas `+DI` estaba por encima de `-DI`.
  - El spread actual debe estar por debajo del parámetro `Max Spread`.
- **Colocación de órdenes**:
  - Las órdenes stop pendientes se colocan `Pips` pasos de precio alejadas del bid o ask actual.
  - Solo una orden pendiente está activa a la vez; las órdenes antiguas se cancelan cuando aparece una nueva señal.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando `-DI` sube por encima de `+DI`.
  - Las posiciones cortas se cierran cuando `+DI` sube por encima de `-DI`.
- **Stops**:
  - Stop-loss y take-profit se aplican a través de `StartProtection` usando los parámetros `Stop Loss` y `Take Profit`.
- **Valores predeterminados**:
  - `ADX Period` = 14
  - `ADX Threshold` = 5
  - `Pips` = 10 pasos de precio
  - `Take Profit` = 1000 pasos de precio
  - `Stop Loss` = 500 pasos de precio
  - `Max Spread` = 20 pasos de precio
  - `Candle Type` = velas de 15 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, DMI
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Intradía
  - Filtro de spread: Sí
