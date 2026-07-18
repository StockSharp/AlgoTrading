# Estrategia de Temporizador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Temporizador recalcula los niveles de ruptura a intervalos de tiempo fijos y opera cuando el precio cruza estos umbrales dinámicos. Los niveles se posicionan usando el Rango Verdadero Promedio (ATR) y una distancia adicional opcional en pips. El enfoque busca capturar rupturas de corto plazo en cualquier dirección.

Cada `WaitSeconds`, la estrategia establece:
- **Nivel de compra** en `close + pipDistance + ATR`.
- **Nivel de venta** en `close - pipDistance - ATR`.

Cuando la siguiente vela completada cierra más allá de uno de estos niveles, se coloca una orden de mercado en la dirección correspondiente. La posición está protegida por stop-loss, take-profit y trailing stop configurables.

El trading puede limitarse a una ventana de tiempo específica usando la configuración de horas de trading.

## Parámetros
- `WaitSeconds` – segundos entre recálculos de niveles.
- `PipDistance` – distancia adicional desde el precio actual, en puntos.
- `AtrPeriod` – período del indicador ATR.
- `TakeProfit` – distancia del take-profit en puntos.
- `StopLoss` – distancia del stop-loss en puntos.
- `TrailingStop` – distancia del trailing stop en puntos.
- `TradeVolume` – volumen de la orden.
- `CandleType` – tipo de vela para los cálculos.
- `UseTradingHours` – habilitar filtro de horario de trading.
- `StartTime` – hora de inicio del trading.
- `StopTime` – hora de fin del trading.

## Cómo funciona
1. Suscripción a velas y cálculo del ATR.
2. En cada vela completada:
   - Si el intervalo de tiempo configurado ha pasado, se calculan nuevos niveles de compra y venta.
   - Si las horas de trading están habilitadas, se verifica que la hora actual esté dentro de la ventana permitida.
   - Se coloca una orden de mercado de compra o venta si el precio cruza el nivel correspondiente.
3. El stop-loss, take-profit y trailing stop son gestionados automáticamente por la infraestructura de la estrategia.

## Notas
- La estrategia opera tanto largo como corto.
- Funciona en cualquier instrumento y marco temporal.
- Los niveles basados en ATR se adaptan a la volatilidad del mercado, permitiendo una detección de rupturas flexible.
