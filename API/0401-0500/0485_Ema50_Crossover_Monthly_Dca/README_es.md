# Estrategia EMA50 Crossover DCA Mensual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA50 Crossover DCA Mensual compra cuando el precio cierra por encima de la EMA de 50 períodos y acumula posiciones adicionales cada mes. Los importes de DCA no invertidos se guardan como efectivo y se despliegan una vez que la tendencia se reanuda.

La estrategia vende cuando el precio cae por debajo de la EMA, cerrando la posición.

## Detalles

- **Criterios de entrada**: cierre > EMA(50)
- **Largo/Corto**: Solo largos
- **Criterios de salida**: el precio cruza por debajo de EMA(50)
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 semana
  - `DcaAmount` = 100000
  - `StartDate` = 1980-01-01
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
