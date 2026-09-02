# Diagrama de la estrategia de cruce de niveles de Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Williams %R indica dónde queda el cierre dentro del rango de las últimas velas, desde 0 arriba hasta -100 abajo. Este diagrama opera el momento en que el oscilador entra en una zona y no aquel en que sale de ella: una caída a través del nivel inferior compra y una subida a través del nivel superior vende. La protección porcentual retira la operación.

![schema](schema.svg)

## Resumen de la estrategia

- El Williams %R de longitud 14 se calcula sobre velas horarias cerradas, que el probador construye a partir del histórico de cinco minutos incluido.
- La señal es el cruce en sí: la lectura anterior a un lado del nivel y la actual al otro, de modo que una estancia larga dentro de la zona dispara una sola vez.
- Se trata de la entrada en la zona, la imagen invertida de la lectura clásica que espera a que el oscilador vuelva a salir, y coincide con el modo Direct de la estrategia original.
- El original incluye además permisos separados para largos y cortos; ambos están activos por defecto, así que el diagrama cablea los dos lados y basta con desconectar una rama para desactivar uno.

## Reglas de entrada y salida

- **Entrada en largo**: El %R anterior estaba por encima del nivel inferior y el actual está en él o por debajo, y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: El %R anterior estaba por debajo del nivel superior y el actual está en él o por encima, y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: El bloque de protección cierra la operación con un take profit del 2 por ciento o un stop loss del 1 por ciento sobre el precio de entrada; antes de eso, el cruce contrario deja la posición en cero, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Williams %R Length | 14 | Longitud de cálculo del Williams %R. |
| Low Level | -80 | Nivel que el oscilador debe atravesar hacia abajo para habilitar una compra. |
| High Level | -20 | Nivel que el oscilador debe atravesar hacia arriba para habilitar una venta. |
| Take profit, % | 2 | Distancia del take profit respecto al precio de entrada, en porcentaje. |
| Stop loss, % | 1 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 01:00:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con el Williams %R, y un bloque de valor anterior guarda la lectura de una vela atrás.
- Cuatro bloques de comparación construyen los dos cruces con la lectura anterior y la actual frente a las dos constantes de nivel.
- Otros dos bloques de comparación contrastan la posición con una constante cero, y cada Y lógica une un cruce con su control de posición.
- Ambos bloques de modificación envían órdenes a mercado con el volumen de una constante compartida, y sus operaciones alimentan el bloque de protección con el take profit y el stop loss.
- El original protege con distancias absolutas de precio; el diagrama usa porcentajes del precio de entrada para que las mismas cifras sirvan en cualquier instrumento.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
