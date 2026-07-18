# Estrategia heredada de captura de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**La estrategia heredada de captura de tendencias** traslada el MetaTrader experto `TrendCapture.mq4` al StockSharp API de alto nivel. La versión C# mantiene el conjunto de reglas original basado en la dirección Parabolic SAR, un filtro bajo ADX y una administración simple del dinero para equilibrar los gastos.

## Ideas centrales
- Procese las velas terminadas del período de tiempo seleccionado y envíelas a Parabolic SAR (`0.02/0.2`) y al índice direccional promedio (`14`).
- Ingrese solo cuando ADX esté por debajo de `AdxThreshold`, lo que indica un mercado tranquilo donde SAR las reversiones son más confiables.
- Recuerde la dirección y el resultado de la última operación cerrada: repita el mismo lado después de un ganador, gire hacia el lado opuesto después de un perdedor.
- Aplique niveles de stop-loss y take-profit de distancia fija (configurados en puntos de precio) y mueva el stop al punto de equilibrio una vez que la operación gane `BreakEvenGuard` puntos.
- Dimensione el volumen del pedido a partir del valor de la cartera disponible y `MaximumRisk`; recurrir a la estrategia `Volume` cuando la información de la cartera no esté disponible.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `SarStep` | 0,02 | Paso de aceleración inicial Parabolic SAR. |
| `SarMax` | 0,2 | Aceleración máxima Parabolic SAR. |
| `AdxPeriod` | 14 | ADX período promedio. |
| `AdxThreshold` | 20 | Valor máximo ADX que aún permite una entrada nueva. |
| `TakeProfitPoints` | 180 | Distancia de obtención de beneficios en puntos de precio. |
| `StopLossPoints` | 50 | Distancia de stop-loss en puntos de precio. |
| `BreakEvenGuard` | 5 | Colchón de ganancias (en puntos) requerido antes de mover la parada a la entrada. |
| `MaximumRisk` | 0,03 | Fracción del margen libre utilizada para dimensionar la posición. |
| `CandleType` | velas de 1 hora | Plazo para cálculos de indicadores y señales comerciales. |

## Gestión de pedidos
- Las entradas largas requieren un precio de cierre superior a SAR con un precio bajo de ADX; los cortos requieren un precio de cierre inferior a SAR con el mismo filtro ADX.
- Los niveles de stop-loss y take-profit se recalculan en cada entrada y se evalúan en cada vela completa.
- La activación del punto de equilibrio simplemente desplaza el tope al precio de entrada. Si no se configura ningún stop-loss (distancia cero o negativa), la guardia se ignora.

## Indicadores
- `ParabolicSar` para sesgo direccional.
- `AverageDirectionalIndex` para el filtro de intensidad (solo se utiliza la línea principal ADX).

## Notas
- La estrategia utiliza `BindEx` para evitar el acceso directo al buffer, siguiendo las pautas del proyecto.
- El cálculo del volumen basado en cartera respeta las restricciones de la junta directiva (`LotStep`, `MinVolume`, `MaxVolume`).
- El historial comercial necesario para el sesgo de dirección se recopila a través de `OnNewMyTrade`, por lo que se siguen admitiendo rellenos parciales.
