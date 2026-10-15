# Estrategia de Canal con Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza entradas por ruptura del canal Donchian y gestión con trailing stop.

El sistema abre operaciones cuando el precio cierra fuera del canal. Un trailing stop sigue el lado opuesto del canal más un desplazamiento. El trailing "lazo" opcional mantiene el stop loss a igual distancia entre el precio actual y el take profit. Las órdenes pendientes pueden eliminarse después de las ejecuciones.

## Detalles

- **Criterios de entrada**: Cierre fuera del rango del canal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Trailing stop o señal opuesta.
- **Stops**: Trailing stop, lazo opcional.
- **Valores predeterminados**:
  - `TrailPeriod` = 5
  - `TrailStop` = 50
  - `UseNooseTrailing` = true
  - `UseChannelTrailing` = true
  - `DeletePendingOrders` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Donchian Channel
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
