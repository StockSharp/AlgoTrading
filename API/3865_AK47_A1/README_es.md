# Estrategia AK47 A1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto del experto "AK47_A1" MetaTrader. La estrategia combina Bill Williams' Alligator, el oscilador DeMarker, el filtro Williams %R y activadores fractales para negociar rupturas solo cuando el mercado abandona las condiciones de rango.

## Detalles
- **Datos**: Velas de precio definidas por `CandleType`.
- **Indicadores**:
  - Alligator mandíbula/dientes/labios son SMMA de período 13/8/5 desplazados en 8/5/3 barras y alimentados con el precio medio.
  - DeMarker con período 13 debe estar en el lado largo de 0,5 para compras y por debajo de 0,5 para ventas.
  - Williams %R con período 14 está normalizado a `[0;1]`; la barra anterior debe permanecer entre 0,25 y 0,75 para evitar estados de sobrecompra/sobreventa.
  - Fractals se detectan en los últimos 5 máximos y mínimos y siguen siendo válidos durante tres barras.
- **Criterios de entrada**:
  - Las tres líneas Alligator deben estar separadas por al menos `SpanGatorPoints` puntos (tanto en alineación alcista como bajista).
  - **Largo**: el fractal inferior más reciente es reciente, DeMarker ≥ 0,5 y el filtro Williams %R aprueba la operación.
  - **Breve**: El fractal superior más reciente es reciente, DeMarker ≤ 0,5 y el filtro Williams %R aprueba la operación.
  - Las posiciones opuestas se aplanan antes de abrir una nueva.
- **Criterios de salida**:
  - Stop-loss estricto y take-profit definidos por `StopLossPoints` y `TakeProfitPoints` (convertidos a precios absolutos mediante el paso del instrumento).
  - Stop dinámico opcional que sigue el cierre en `TrailingStopPoints` puntos una vez que la posición se mueve a favor.
  - Cuando aparece una señal de marcha atrás, la posición actual se cierra antes de abrir la nueva.
- **Valores predeterminados**:
  - `SpanGatorPoints` = 0,5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0 (deshabilitado)
  - `TrailingStopPoints` = 50
  - `CandleType` = velas de 1 hora
