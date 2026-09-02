# Diagrama de la estrategia de ruptura del canal del OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El On-Balance Volume suma el volumen de cada vela alcista y resta el de cada vela bajista, de modo que su curva es el saldo acumulado de la presión compradora frente a la vendedora. Este diagrama coloca un canal al estilo Donchian sobre esa curva y no sobre el precio: cuando el OBV sale del canal de las velas anteriores por arriba, manda la acumulación y el esquema compra; cuando sale por abajo, manda la distribución y el esquema vende.

![schema](schema.svg)

## Resumen de la estrategia

- El canal lo forman un bloque Highest y otro Lowest de 60 valores, alimentados por el bloque On-Balance Volume y no por las velas.
- Dos bloques de valor anterior conservan el canal de la vela precedente, así que la ruptura se mide contra un borde que el valor actual del OBV todavía no ha desplazado.
- Como el borde viene de la vela anterior, la ruptura es un suceso y no un estado: opera justo la vela que empuja el OBV más allá del extremo previo.
- La estrategia original lleva el ATR en el nombre, pero su propio código nunca usa ese indicador, así que el diagrama lo omite y conserva solo lo que realmente decide una operación.

## Reglas de entrada y salida

- **Entrada en largo**: El valor actual del OBV está por encima del techo del canal de la vela anterior y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: El valor actual del OBV está por debajo del suelo del canal de la vela anterior y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: El bloque de protección cierra la operación con un take profit del 5 por ciento o un stop loss del 3 por ciento sobre el precio de entrada; la ruptura contraria también deja la posición en cero, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Channel Length | 60 | Número de valores del OBV en la ventana de Highest y Lowest; ambos bloques deben llevar la misma longitud. |
| Take profit, % | 5 | Distancia del take profit respecto al precio de entrada, en porcentaje. |
| Stop loss, % | 3 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque On-Balance Volume, cuya salida pasa al bloque Highest y al Lowest: un indicador que lee a otro indicador.
- Cada borde del canal atraviesa un bloque de valor anterior, de modo que la comparación usa el borde de la vela previa a la ruptura.
- Dos bloques de comparación miden el OBV actual contra esos bordes y otros dos comparan la posición con una constante cero; cada Y lógica une una ruptura con su control de posición.
- El original mantiene un régimen alcista o bajista pegajoso y opera solo cuando cambia; el diagrama logra la misma entrada única por tramo con el control de posición, que bloquea una ruptura repetida en el sentido en el que ya está posicionado.
- Ambos bloques de modificación envían órdenes a mercado con el volumen de una constante compartida, y sus operaciones alimentan el bloque de protección con el take profit y el stop loss.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
