# Estrategia de Stop Trailing y Take
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Stop Trailing y Take** es una adaptación directa de StockSharp del asesor experto de MetaTrader de `MQL/19963`. Se centra en la gestión activa de operaciones: una vez que una posición está abierta, la estrategia adjunta niveles iniciales de stop-loss y take-profit y luego sigue ambos niveles a medida que el precio se mueve. Los ajustes de trailing respetan tamaños mínimos de paso configurables, protección de break-even y la opción de evitar el trailing mientras una operación todavía está en pérdida.

La estrategia opera en un único instrumento usando velas terminadas. Cuando la estrategia está plana, abre una posición en la dirección del cuerpo de la vela más reciente (los cierres alcistas conducen a largos, los cierres bajistas conducen a cortos). Esto refleja el comportamiento de prueba original utilizado por el script MQL y proporciona un flujo continuo de posiciones para que el motor de trailing gestione.

## Cómo funciona
1. Suscribirse al tipo de vela configurado y procesar solo velas terminadas.
2. Cuando no hay posición abierta, entrar largo en velas alcistas o corto en velas bajistas (respetando el filtro de tipo de posición).
3. En una nueva posición, inicializar las distancias de stop-loss y take-profit usando `InitialStopLossPoints`/`InitialTakeProfitPoints`. Si son cero, se usan las distancias de trailing en su lugar.
4. En cada cierre de vela, calcular los objetivos de trailing actualizados:
   - Los stops se acercan al precio solo después de que el mercado avanza por el paso de trailing.
   - Los take-profits se acercan cuando el precio retrocede al menos el paso de trailing.
   - La protección de break-even evita mover los niveles a una zona de pérdida cuando `AllowTrailingLoss` está deshabilitado.
5. Cuando el precio cruza un stop trailing o nivel de take-profit, salir con orden de mercado y restablecer todos los niveles almacenados.

## Lógica de trailing
### Posiciones largas
- El stop inicial se limita a al menos `SpreadMultiplier * PriceStep` de distancia de la entrada.
- El take-profit inicial se posiciona al menos la misma distancia mínima por encima de la entrada.
- El trailing stop sigue el precio de cierre a la baja por `TrailingStopLossPoints` mientras respeta el paso de trailing y el filtro de break-even opcional.
- El trailing take-profit se ajusta cuando el precio retrocede, nunca moviéndose por debajo del nivel de break-even cuando el trailing en pérdidas está deshabilitado.

### Posiciones cortas
- El stop inicial se establece por encima de la entrada, no más cerca que la distancia del multiplicador de spread.
- El take-profit inicial comienza por debajo de la entrada con la misma regla de distancia mínima.
- El trailing stop baja cuando el precio cae, pero no se moverá más alto que el break-even a menos que se permita el trailing en pérdidas.
- El trailing take-profit sube hacia el precio en los retrocesos, limitado al break-even cuando es necesario.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Agregación de velas usada para la evaluación de precios. |
| `Volume` | Volumen de orden predeterminado para entradas y salidas. |
| `PositionType` | Restringe el motor a gestionar posiciones largas, cortas o ambas. |
| `InitialStopLossPoints` | Tamaño inicial del stop-loss en puntos de precio (usa la distancia de trailing si es cero). |
| `InitialTakeProfitPoints` | Tamaño inicial del take-profit en puntos de precio (usa la distancia de trailing si es cero). |
| `TrailingStopLossPoints` | Distancia entre el precio y el trailing stop. |
| `TrailingTakeProfitPoints` | Distancia entre el precio y el trailing take-profit. |
| `TrailingStepPoints` | Movimiento mínimo en puntos requerido antes de ajustar stops o objetivos. |
| `AllowTrailingLoss` | Habilita el trailing mientras la operación todavía está por debajo del break-even. |
| `BreakevenPoints` | Desplazamiento en puntos añadido al precio de entrada para formar la barrera de break-even. |
| `SpreadMultiplier` | Multiplicador para la aproximación de distancia mínima del stop (simula el `StopLevel` de MQL). |

## Notas
- Los stops y objetivos se ejecutan con órdenes de mercado cuando se activan, lo que mantiene la implementación simple y refleja las modificaciones originales de stop.
- `SpreadMultiplier` aproxima el comportamiento de MQL donde los niveles de stop no pueden colocarse más cerca que el spread actual. Ajustar este valor para coincidir con el venue de ejecución.
- La estrategia evita intencionalmente una versión de Python y se centra exclusivamente en la implementación C#, según lo solicitado.
- Considere combinar el motor de trailing con su propio filtro de entrada deshabilitando las entradas integradas e inyectando órdenes externas si es necesario.
