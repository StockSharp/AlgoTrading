# Estrategia MACD EA sencilla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Simple MACD EA es una adaptación directa del clásico asesor experto MetaTrader "Simple MACD EA". El enfoque utiliza dos promedios móviles exponenciales (EMA) para emular el histograma MACD y determinar la tendencia dominante en velas de un minuto. Las posiciones largas se abren cuando el EMA rápido (período 100) cruza por encima del EMA lento (nivel MACD definido por el usuario). Las posiciones cortas se abren cuando el EMA rápido cae por debajo del EMA lento. Sólo se mantiene una posición en cualquier momento.

## Lógica de gestión comercial
- **Detección de tendencias:** La diferencia entre el EMA de 100 períodos y el MACD EMA configurable define la dirección de la tendencia actual (`+1`, `0`, `-1`). Una reversión de negativo a positivo abre una posición larga. Una reversión de positivo a negativo abre una posición corta.
- **Confirmación de impulso:** La estrategia realiza un seguimiento de la diferencia entre el MACD EMA y un EMA (`MACD level + 1`) ligeramente más lento. Si la diferencia con respecto a la operación actual se reduce después de que el precio se haya movido al menos cinco puntos en beneficio, la posición se cierra anticipadamente.
- **Protección basada en el tiempo:** Después de que una operación permanece abierta durante un número de ciclos de evaluación definido por el usuario, el sistema activa una parada suave que reduce la tolerancia al movimiento adverso del precio en relación con el precio de entrada.
- **Salida dinámica:** Una vez que la operación genera ganancias y permanece activa durante suficientes ciclos, se activa un stop dinámico interno. El nivel de parada sigue el precio según la cantidad de puntos configurados y se puede actualizar una cantidad limitada de veces. Si se alcanza el límite, la posición se cierra.
- **Salida de inversión de tendencia:** Cuando la señal de tendencia cambia en la dirección opuesta mientras el precio ya tiene cinco puntos de ganancia, la posición se cierra inmediatamente.

## Parámetros
- **Tipo de vela**: período de tiempo utilizado para los cálculos EMA (predeterminado: velas de 1 minuto).
- **Volumen** – Volumen de pedidos para nuevas entradas.
- **MACD Nivel**: longitud de EMA que define el componente lento MACD. Un EMA secundario con longitud `MACD Level + 1` se deriva automáticamente.
- **Trailing Stop** – Distancia en puntos para la salida final. Establezca en cero para desactivar.
- **Actualizaciones finales**: número máximo de ajustes de trailing stop por operación.
- **Ciclos de espera**: número de evaluaciones de velas que se deben esperar antes de que se active la parada suave adaptativa.

## Notas adicionales
- La estrategia siempre aplana la posición actual antes de invertir la dirección.
- La información del paso de precio del valor seleccionado se utiliza para traducir distancias basadas en puntos en precios reales.
- La implementación se basa en la suscripción de vela de alto nivel de StockSharp API y no pone en cola los buffers de indicadores personalizados.
