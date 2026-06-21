# Estrategia Exp Martin V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Exp Martin V2 implementa un enfoque martingala exponencial. Siempre mantiene una sola posición abierta y después de cada operación decide la siguiente dirección y volumen en función del beneficio de la última transacción.

La estrategia comienza con un tipo de orden predefinido (compra o venta) y un volumen inicial. Se aplica un take-profit y un stop-loss fijos a cada posición. Cuando una operación se cierra con beneficio, se abre una nueva posición del mismo tipo con el volumen inicial. Si la operación termina con pérdida, la dirección se invierte y el volumen se multiplica por un factor especificado. La multiplicación continúa después de cada pérdida hasta que se alcanza un número máximo de multiplicaciones; entonces el volumen se restablece al valor inicial.

Esto crea una secuencia escalada de operaciones opuestas que busca recuperar las pérdidas anteriores una vez que ocurre un movimiento rentable.

## Detalles

- **Lógica de entrada**:
  - Abrir la posición inicial según *Start Type* (0 - compra, 1 - venta) con el *Start Volume*.
  - Después de una operación rentable, repetir la misma dirección con el volumen inicial.
  - Después de una operación perdedora, invertir la dirección y multiplicar el volumen por *Factor* hasta alcanzar las multiplicaciones de *Limit*.
- **Largo/Corto**: Ambos, dependiendo de la secuencia actual.
- **Lógica de salida**:
  - Las posiciones se cierran cuando el precio alcanza los niveles configurados de *Take Profit* o *Stop Loss*.
- **Stops**: Stop-loss y take-profit fijos en puntos.
- **Filtros**: Ninguno.
- **Gestión de posición**: Solo una posición abierta a la vez.

Utilice esta estrategia para experimentar con la gestión de dinero martingala en StockSharp sin indicadores adicionales.
