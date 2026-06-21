# Estrategia Exp CyclePeriod
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador CyclePeriod para detectar giros del ciclo de mercado. Abre posiciones largas cuando el indicador sube y posiciones cortas cuando baja, cerrando las posiciones opuestas en consecuencia.

## Detalles

- **Criterios de entrada:**
  - **Largo**: CyclePeriod está subiendo y el valor actual está por encima del anterior.
  - **Corto**: CyclePeriod está bajando y el valor actual está por debajo del anterior.
- **Largo/Corto**: Largo y Corto.
- **Criterios de salida:**
  - Cerrar corto cuando CyclePeriod gira hacia arriba.
  - Cerrar largo cuando CyclePeriod gira hacia abajo.
- **Stops**: Utiliza take profit y stop loss en unidades de precio.
- **Valores predeterminados:**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true.
  - `SellPosClose` = true.
- **Filtros:**
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: CyclePeriod
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 6 horas
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
