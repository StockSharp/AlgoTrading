# OsHMA Rompimiento Twist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia construida sobre el oscilador OsHMA (diferencia entre las Hull Moving Averages rápida y lenta). Puede operar en dos modos:

- **Breakdown** – opera cuando el oscilador cruza la línea cero.
- **Twist** – opera cuando el oscilador cambia de dirección.

La estrategia se suscribe a velas del marco temporal seleccionado y utiliza indicadores de Hull Moving Average para calcular el oscilador.

## Detalles

- **Criterios de entrada**: Cruce de cero de OsHMA o cambio de dirección.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Take profit y stop loss.
- **Valores predeterminados**:
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
