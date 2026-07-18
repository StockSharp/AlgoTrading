# Estrategia Exp XPeriodCandle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del asesor experto MQL5 `Exp_XPeriodCandle`. Reconstruye el indicador personalizado XPeriodCandle con componentes de API de alto nivel y utiliza transiciones de color de velas para abrir y cerrar posiciones.

## Concepto

* Suavizar la apertura, máximo, mínimo y cierre de cada vela terminada usando una aproximación de media móvil configurable.
* Registrar el "color de vela" resultante (alcista si el cierre suavizado está por encima de la apertura suavizada, bajista en caso contrario).
* Usar el color de las últimas dos velas completadas (desplazamiento configurable) para detectar reversiones y emitir señales de trading.
* Opcionalmente cerrar posiciones opuestas cuando aparece una nueva señal y aplicar niveles de stop-loss/take-profit protectores expresados en puntos de precio.

## Detalles de implementación

* Tipos de suavizado directamente soportados: Simple, Exponencial, Suavizado (RMA) y Ponderado Lineal. Todas las demás opciones se aproximan con un suavizador exponencial porque StockSharp no incluye equivalentes directos de JJMA/JurX/Parabolic/T3/VIDYA/AMA. Documentado en comentarios de código para mantener el comportamiento transparente.
* Las colas deslizantes almacenan los últimos `Period` máximos y mínimos suavizados para mantener el rango de precio consistente con el indicador original.
* La estrategia espera hasta que haya suficiente historial disponible antes de llamar a `BuyMarket`/`SellMarket` y se marca como formada para trabajar con los filtros de backtesting de StockSharp.
* Las conversiones opcionales de deslizamiento, stop-loss y take-profit dependen del paso de precio del instrumento. Cuando el paso es desconocido se usan los valores de puntos brutos.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal de las velas procesadas. |
| `Period` | Profundidad de la ventana de suavizado (igual que el período del indicador). |
| `SmoothingMethods` | Aproximación de media móvil usada para todas las series OHLC. Los métodos no soportados vuelven a EMA. |
| `SmoothingLength` | Parámetro de longitud para el suavizador. |
| `SmoothingPhase` | Entrada de fase adicional (mantenida para completitud; solo activa en la familia JJMA original de MQL). |
| `SignalBar` | Qué vela completada evaluar (1 = vela anterior, replicando el valor predeterminado del experto MQL). |
| `EnableLongEntry` / `EnableShortEntry` | Permitir abrir posiciones en la dirección correspondiente. |
| `EnableLongExit` / `EnableShortExit` | Cerrar posiciones existentes cuando se detecta una señal opuesta. |
| `StopLossPoints` / `TakeProfitPoints` | Salidas protectoras expresadas en puntos de precio. Establecer en cero para deshabilitar. |
| `SlippagePoints` | Deslizamiento permitido en puntos de precio aplicado a órdenes de mercado. |

## Reglas de trading

1. Suavizar la última vela terminada y agregar su color al historial rotativo.
2. Cuando existen los colores de `SignalBar` y anteriores:
   * Si la vela más antigua era alcista (color < 1) y la vela más nueva es no alcista (color > 0), abrir posición larga (si está permitido) y opcionalmente cerrar cortos.
   * Si la vela más antigua era bajista (color > 1) y la vela más nueva es no bajista (color < 2), abrir posición corta (si está permitido) y opcionalmente cerrar largos.
3. El tamaño de posición sigue la configuración `Volume` de la estrategia; la exposición opuesta se aplana antes de revertir.
4. La gestión de riesgo es manejada por `StartProtection` usando las distancias de puntos proporcionadas.

## Notas

* El experto original usa el `SmoothAlgorithms.mqh` propietario. Dado que StockSharp carece de implementaciones directas de JJMA/JurX/T3, la conversión en C# aproxima esos modos con suavizado exponencial. Este comportamiento está documentado en comentarios de código y el README para que los optimizadores puedan ajustar los parámetros si es necesario.
* Las entradas y valores predeterminados reflejan la versión MQL, permitiendo rangos de optimización similares.
