# Estrategia VR Overturn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia VR Overturn** implementa una lógica simple de martingala/anti-martingala.
Siempre mantiene una sola posición y, una vez cerrada, abre inmediatamente una nueva
basándose en el resultado de la operación anterior.

## Lógica de la estrategia

1. Abrir la primera posición según `StartSide` con volumen `StartVolume`.
2. Adjuntar stop-loss y take-profit usando desplazamientos en puntos.
3. Cuando la posición se cierra:
   - Calcular el beneficio de la última operación.
   - Para el modo **Martingale**:
     - Después de una operación rentable, restablecer el volumen a `StartVolume` y mantener la misma dirección.
     - Después de una operación con pérdida, multiplicar el volumen por `Multiplier` e invertir la dirección.
   - Para el modo **AntiMartingale**:
     - Después de una operación rentable, multiplicar el volumen por `Multiplier` y mantener la misma dirección.
     - Después de una operación con pérdida, restablecer el volumen a `StartVolume` e invertir la dirección.
4. Abrir la siguiente posición usando la dirección y el volumen calculados.

El proceso se repite indefinidamente mientras la estrategia está en ejecución.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Mode` | Modo de trading: `Martingale` o `AntiMartingale`. |
| `StartSide` | Lado de la primera operación (`Buy` o `Sell`). |
| `TakeProfit` | Valor de take-profit en puntos desde el precio de entrada. |
| `StopLoss` | Valor de stop-loss en puntos desde el precio de entrada. |
| `StartVolume` | Volumen inicial utilizado para la primera orden. |
| `Multiplier` | Multiplicador aplicado al volumen después de un beneficio o pérdida. |

## Notas

- Las órdenes de protección se registran como órdenes stop y límite.
- En todo momento solo existe una posición.
- La estrategia no utiliza ningún indicador de mercado.
