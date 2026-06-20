# Hull Ma Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en Hull Moving Average y ADX. Entra en largo cuando el HMA sube y ADX > 25 (tendencia fuerte). Entra en corto cuando el HMA baja y ADX > 25 (tendencia fuerte). Sale cuando ADX < 20 (tendencia debilitándose).

Las pruebas indican un rendimiento anual promedio de aproximadamente 178%. Funciona mejor en el mercado de acciones.

Hull MA muestra la tendencia, mientras que ADX confirma su intensidad. Las entradas siguen la pendiente de Hull cuando ADX indica fortaleza.

Efectiva para operadores que se centran en tendencias suaves con confirmación. Los stops basados en ATR mantienen las pérdidas bajo control.

## Detalles

- **Criterios de entrada**:
  - Largo: `HullMA turning up && ADX > 25`
  - Corto: `HullMA turning down && ADX > 25`
- **Largo/Corto**: Ambos
- **Criterios de salida**: reversión de Hull MA
- **Stops**: Basados en ATR usando `AtrMultiplier`
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Hull MA, Moving Average, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

