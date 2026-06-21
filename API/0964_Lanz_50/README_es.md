# Estrategia LANZ 5.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia LANZ 5.0 opera en la dirección de una EMA de 200 períodos y requiere tres velas consecutivas del mismo color. Limita las operaciones por conteo diario, ventana horaria de Nueva York y distancia mínima entre entradas.

## Detalles

- **Criterios de entrada**:
  - Precio por encima de la EMA y tres velas alcistas para entradas largas.
  - Precio por debajo de la EMA y tres velas bajistas para entradas cortas (opcional).
- **Largo/Corto**: Largo por defecto.
- **Criterios de salida**:
  - Stop-loss o take-profit fijos.
  - Cierre manual a la hora configurada.
- **Stops**:
  - Stop loss = 40 pips.
  - Take profit = 120 pips.
- **Valores predeterminados**:
  - `EmaPeriod` = 200
  - `MaxTrades` = 99
  - `MinDistancePips` = 25
  - `StopLossPips` = 40
  - `TakeProfitPips` = 120
  - `StartHour` = 19
  - `EndHour` = 15
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
