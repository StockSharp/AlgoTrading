# Estrategia Gap DM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
Gap DM es una estrategia de trading de gaps contrarian que rastrea la distancia entre el cierre de la sesión anterior y la apertura de la siguiente sesión. Cuando el mercado abre con un gap visible, la estrategia opera inmediatamente en la dirección opuesta, esperando que el precio revierta y llene el gap. La implementación sigue el algoritmo original de MetaTrader 5 "Gap DM" de cmillion, adaptado a la API de alto nivel de StockSharp. Todas las decisiones de trading se derivan de velas completadas del marco temporal seleccionado, asegurando un comportamiento determinista en backtests y ejecución en vivo.

## Lógica de señal
1. Suscribirse a la serie de velas especificada por `CandleType`.
2. Esperar que cada vela termine (`CandleStates.Finished`).
3. Comparar el precio de cierre de la vela anterior con el precio de apertura de la vela actual.
4. Calcular el tamaño del gap en pips usando el paso de precio del instrumento. Un multiplicador de 10 se aplica automáticamente para cotizaciones de 3 y 5 dígitos, reproduciendo la conversión punto-a-pip de MT5.
5. Si la apertura actual está **por debajo** del cierre anterior por al menos `Minimum Gap (pips)`, tratar como un gap bajista y **entrar en largo**.
6. Si la apertura actual está **por encima** del cierre anterior por al menos `Minimum Gap (pips)`, tratar como un gap alcista y **entrar en corto**.
7. Omitir entradas cuando el trading no está permitido (por ejemplo, la estrategia está desconectada o todavía en calentamiento).

## Dimensionamiento de posición y límites
- `Order Volume` especifica el tamaño de lote para cada nueva operación. La estrategia también usa el valor para cerrar o revertir exposición existente, manteniendo la posición neta consistente con el modelo de contabilidad neta de StockSharp.
- `Max Positions` define el volumen máximo agregado (en lotes) que se puede mantener en una dirección. Cuando se alcanza el límite, las nuevas entradas en la misma dirección se ignoran.
- Al revertir de corto a largo (o viceversa), la estrategia agrega automáticamente el volumen necesario para cerrar la exposición opuesta antes de abrir la nueva posición.

## Gestión de riesgo
- `Stop Loss (pips)` coloca un stop protector relativo al precio de entrada. El stop se evalúa en cada vela completada. Si el rango de la vela toca el nivel de stop, la posición se cierra inmediatamente con una orden de mercado.
- `Take Profit (pips)` funciona simétricamente al stop-loss. Establecer el parámetro en cero para deshabilitar el objetivo.
- No se aplica trailing stop por defecto; la lógica de salida coincide con el Expert Advisor fuente.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Order Volume` | Volumen de trading usado para cada entrada en lotes. | `1` |
| `Stop Loss (pips)` | Distancia del stop protector. Establecer en `0` para deshabilitar. | `0` |
| `Take Profit (pips)` | Distancia del objetivo de beneficio. Establecer en `0` para deshabilitar. | `0` |
| `Minimum Gap (pips)` | Diferencia mínima entre el cierre anterior y la apertura actual requerida para generar una señal. | `1` |
| `Max Positions` | Exposición acumulada máxima permitida en una sola dirección (en lotes). | `15` |
| `Candle Type` | Marco temporal usado para medir gaps de sesión. | `1 Hora` |

## Flujo de ejecución
1. Restablecer el estado cacheado en cada reinicio (umbrales de gap, niveles de stop, cierre anterior).
2. Iniciar la suscripción de velas y dibujar elementos del gráfico (velas y operaciones) cuando hay un área de gráfico disponible.
3. En cada vela terminada:
   - Actualizar o restablecer el stop activo y el objetivo dependiendo de la posición actual.
   - Evaluar las condiciones de gap y colocar órdenes de mercado cuando aparece una señal válida.
   - Volver a verificar las órdenes protectoras para que los eventos de stop-loss o take-profit dentro de la misma vela se manejen sin demora.
4. Almacenar el último cierre para la siguiente evaluación.

## Notas y diferencias vs. la versión MT5 original
- Las estrategias de StockSharp operan con posiciones netas. El algoritmo emula múltiples entradas escalando la exposición neta en lugar de crear tickets separados. Esto mantiene el comportamiento cercano al Expert Advisor de MT5 mientras respeta el modelo de contabilidad de StockSharp.
- Todos los comentarios en el código fuente están en inglés, en concordancia con las pautas del proyecto.
- La gestión monetaria mediante porcentaje de riesgo (modo `risk` en el script MT5) no se reproduce; en cambio, se proporciona un parámetro de volumen fijo. Establecer `Order Volume` al tamaño de lote que desea operar.

## Requisitos
- Compatible con cualquier instrumento que exponga un `PriceStep` válido.
- Funciona con velas basadas en tiempo, volumen o rango soportadas por StockSharp, siempre que el concepto de gap sea significativo.
- Requiere un entorno StockSharp capaz de ejecutar órdenes de mercado y monitorear operaciones propias.
