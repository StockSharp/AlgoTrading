# Estrategia MOC Delta MOO Entry v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia registra el volumen de compra y venta durante la sesión de la tarde y utiliza el delta MOC resultante para operar la apertura del día siguiente.

De 14:50 a 14:55 acumula máximos, mínimos y volumen separado de compra/venta. A las 14:55 calcula el porcentaje del delta de compra menos venta relativo al volumen diario total. A las 8:30 del día siguiente se abre una operación larga si el delta está por encima del umbral y la apertura está por encima de las SMA de 15 y 30 períodos. Una operación corta usa las condiciones opuestas. Las posiciones incluyen take profit y stop loss basados en ticks y se cierran a las 14:50.

## Detalles

- **Criterios de entrada**: A las 8:30, porcentaje del delta por encima del umbral y precio por encima de SMA15 y SMA30 para largo; delta por debajo del umbral negativo y precio por debajo de las SMA para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take profit o stop loss; todas las posiciones se cierran a las 14:50.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `DeltaThreshold` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Volumen, SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
