# Estrategia Yesterday Today
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Yesterday Today reproduce el clásico breakout de MetaTrader donde el precio actual se compara con el máximo y mínimo de ayer. La estrategia rastrea la última vela diaria completada y observa las velas intradía para reaccionar rápidamente cuando el precio escapa del rango de ayer. Antes de revertir, siempre cierra cualquier exposición opuesta, lo que resulta en un flujo de trabajo limpio de una sola posición.

## Resumen

- Rastrea el rango diario anterior y espera el cierre de una vela intradía para romperlo.
- Abre posiciones largas cuando el cierre supera el máximo de ayer; abre posiciones cortas cuando el cierre cae por debajo del mínimo de ayer.
- Aplica niveles de stop-loss y take-profit a distancia fija expresados en pips. El tamaño del pip se adapta a cotizaciones forex de 3 o 5 dígitos, tal como en la implementación MQL original.
- Los niveles de riesgo se evalúan en cada vela intradía terminada usando su máximo/mínimo para detectar alcances de stop-loss o take-profit.
- Utiliza el framework de protección integrado para protegerse contra problemas inesperados de margen.

## Flujo de trabajo

1. Suscribirse a velas diarias y almacenar el máximo/mínimo de la última sesión completada.
2. Suscribirse a velas intradía (15 minutos por defecto) para evaluación de señales.
3. En cada vela intradía terminada:
   - Salir inmediatamente si la vela viola el stop-loss o take-profit activo.
   - Entrar en largo si el cierre está por encima del máximo de ayer y no hay posición larga abierta.
   - Entrar en corto si el cierre está por debajo del mínimo de ayer y no hay posición corta abierta.
   - Cualquier posición opuesta se cierra primero aumentando el volumen de la orden de mercado.
4. Cada vez que se complete una nueva vela diaria, actualizar el rango almacenado para el siguiente día de operaciones.

## Parámetros

- `TradeVolume` — tamaño de lote para nuevas posiciones. Al revertir, la estrategia añade automáticamente la exposición opuesta para aplanar primero.
- `StopLossPips` — distancia desde el precio de entrada al stop protector, expresada en pips. Un valor de `0` desactiva el stop.
- `TakeProfitPips` — distancia desde el precio de entrada al objetivo de beneficio, expresada en pips. Un valor de `0` desactiva el objetivo.
- `SignalCandleType` — tipo de vela intradía utilizado para la detección de ruptura (por defecto velas de 15 minutos).

## Detalles

- **Criterios de entrada**: La vela intradía cierra por encima del máximo de ayer (largo) o por debajo del mínimo de ayer (corto).
- **Largo/Corto**: Ambas direcciones soportadas.
- **Criterios de salida**: Niveles de stop-loss o take-profit tocados por los extremos de la vela intradía.
- **Stops**: Sí, distancias fijas en pips.
- **Valores predeterminados**:
  - `TradeVolume` = 1
  - `StopLossPips` = 50
  - `TakeProfitPips` = 50
  - `SignalCandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Entradas intradía con contexto diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Notas

- La estrategia está diseñada para un único instrumento. Configure `Security` y `Portfolio` antes de iniciar.
- El tamaño del pip se calcula a partir de `Security.PriceStep` y se escala automáticamente para símbolos forex de 3 o 5 decimales, replicando la lógica original del EA.
- La protección se activa en `OnStarted`, de modo que las salvaguardas globales de la cuenta permanecen activas cuando la estrategia opera.
