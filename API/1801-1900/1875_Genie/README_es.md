# Estrategia Genie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Genie es un asesor experto basado en Parabolic SAR mejorado con el Índice Direccional Promedio (ADX) para confirmar la fuerza de la tendencia. La estrategia abre posiciones cuando el SAR cambia de dirección respecto al precio mientras los componentes +DI y -DI del ADX intercambian su dominancia. Un stop trailing y un take profit fijo gestionan el riesgo.

Las pruebas demuestran que el enfoque funciona mejor en instrumentos con tendencia y volatilidad moderada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SAR anterior por encima del cierre previo, SAR actual por debajo del cierre actual, +DI anterior < -DI anterior, +DI actual > -DI actual, y ADX por encima del +DI y -DI actuales.
  - **Corto**: SAR anterior por debajo del cierre previo, SAR actual por encima del cierre actual, +DI anterior > -DI anterior, +DI actual < -DI actual, y ADX por encima del +DI y -DI actuales.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El stop trailing se activa o la vela anterior cierra contra la posición.
- **Stops**: Sí, stop trailing y take profit medidos en unidades de precio.
- **Valores predeterminados**:
  - `TakeProfit` = 500
  - `TrailingStop` = 200
  - `SarStep` = 0.02
  - `AdxPeriod` = 14
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (entre +DI y -DI)
  - Nivel de riesgo: Medio
