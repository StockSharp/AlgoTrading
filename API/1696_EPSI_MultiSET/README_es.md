# Estrategia EPSI Multi SET
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura convertida del experto MQL4 original *e-PSI@MultiSET*.
Observa cada vela y entra cuando el precio se desplaza una distancia especificada desde la apertura.
Las posiciones utilizan niveles de take-profit y stop-loss y las operaciones solo están permitidas durante
una ventana de tiempo definida por el usuario.

## Detalles

- **Criterios de entrada**:
  - Largo: `High - Open >= MinDistance`
  - Corto: `Open - Low >= MinDistance`
- **Largo/Corto**: Ambos
- **Criterios de salida**: TakeProfit o StopLoss
- **Stops**: Sí
- **Valores predeterminados**:
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
