# Diagrama de la estrategia de sobrecompra y sobreventa del estocástico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La línea %K del estocástico mide dónde queda el cierre dentro del rango reciente de máximos y mínimos, y este diagrama opera contra los extremos de ese rango. Lo que importa es el momento en que %K entra en una zona, no todo el tiempo que pasa dentro, así que un bloque de valor anterior convierte la prueba de nivel en un cruce y cada señal produce una sola orden.

![schema](schema.svg)

## Resumen de la estrategia

- La línea %K se calcula sobre velas cerradas de un solo instrumento; la línea suavizada %D no participa en la decisión, igual que en la estrategia original.
- Una ventana de tres velas hace de %K una línea muy rápida: alcanza ambas zonas con frecuencia, y de ahí viene el número de operaciones de este ejemplo.
- Los niveles de sobreventa y sobrecompra son constantes del diagrama, así que pueden editarse y optimizarse; en el código original están fijados en 20 y 80.
- Todas las órdenes usan el mismo volumen, de modo que una señal contraria a la posición abierta la cierra en lugar de invertirla y agrandarla.

## Reglas de entrada y salida

- **Entrada en largo**: La lectura anterior de %K estaba en el nivel de sobreventa o por encima, la actual está por debajo y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: La lectura anterior de %K estaba en el nivel de sobrecompra o por debajo, la actual está por encima y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay bloque de salida propio: el cruce contrario cierra la posición, porque todas las órdenes usan el mismo volumen. La estrategia original además hace una pausa de un número fijo de velas tras cada operación; no existe un bloque contador de velas, así que el cruce asume ese papel y evita que el diagrama dispare en cada vela dentro de la zona.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| %K Length | 3 | Ventana de máximos y mínimos con la que se mide la línea %K. |
| Oversold | 20 | Nivel que la línea %K debe cruzar a la baja para comprar. |
| Overbought | 80 | Nivel que la línea %K debe cruzar al alza para vender. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque del indicador %K, cuya salida va tanto a los bloques de comparación como a un bloque de valor anterior.
- Cuatro bloques de comparación construyen los dos cruces: la lectura anterior frente a un nivel y la actual frente al mismo nivel.
- El bloque de posición se compara dos veces con una constante cero, dando un control de «no largo» para la compra y de «no corto» para la venta.
- Cada Y lógica une las dos mitades de un cruce con su control de posición y dispara un bloque de modificación de posición; ambos toman el volumen de una constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
