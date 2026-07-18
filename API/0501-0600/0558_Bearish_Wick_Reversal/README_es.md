# Estrategia de Reversión por Mecha Bajista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando una vela bajista forma una larga mecha inferior que supera un umbral porcentual definido por el usuario. Un filtro EMA opcional requiere que el cierre esté por encima de una media móvil para confirmar la dirección de la tendencia. Las posiciones se cierran cuando el precio cierra por encima del máximo de la vela anterior.

## Detalles

- **Criterios de entrada:** vela bajista con mecha inferior <= umbral y dentro de la ventana de operación; opcionalmente precio por encima de EMA.
- **Largo/Corto:** Solo largos.
- **Criterios de salida:** precio de cierre > máximo anterior.
- **Stops:** Ninguno.
- **Valores predeterminados:**
  - Umbral = -1 (%)
  - Filtro EMA desactivado, período EMA = 200
  - Hora de inicio = 2014-01-01, Hora de fin = 2099-01-01
  - Marco temporal de velas = 1 minuto
- **Filtros:**
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
