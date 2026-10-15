# Estrategia de Operador en Múltiples Marcos Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea la lógica MQL original del "Multi Time Frame Trader" con las APIs de alto nivel de StockSharp. Combina tres
canales de regresión polinómica (M1, M5 y H1) y sólo opera cuando los marcos temporales inferiores prueban los extremos de su canal en
la dirección sugerida por la pendiente horaria.

El sistema recalcula continuamente las bandas superior, media e inferior del canal de regresión en cada vela completada. Cuando la
banda superior horaria disminuye, el sesgo es bajista; cuando sube, el sesgo es alcista. Las entradas se activan una vez que las velas
M5 y M1 alcanzan la banda correspondiente y el filtro direccional coincide.

## Flujo de trabajo principal

- **Suscripciones**: la estrategia escucha velas de 1 minuto, 5 minutos y 1 hora simultáneamente.
- **Canal de regresión**: cada suscripción construye una línea de regresión polinómica (grado 1-3) sobre `Bars` puntos y la desplaza
  `StdMultiplier` desviaciones estándar para obtener bandas de resistencia y soporte.
- **Estimación de pendiente**: la pendiente del canal se deriva de la diferencia entre la banda superior actual y la banda superior
  hace `Bars` velas, reflejando el comportamiento del indicador `i-Regr`.
- **Filtro direccional**: la pendiente H1 define si sólo se permiten posiciones cortas (pendiente negativa) o largas (pendiente positiva).

## Lógica de entrada

### Operaciones cortas

1. La pendiente horaria es negativa.
2. El máximo de la última vela de 5 minutos toca o rompe la resistencia de regresión de 5 minutos.
3. El máximo de la última vela de 1 minuto toca o rompe la resistencia de 1 minuto.
4. No hay posición corta existente abierta (`Position >= 0`).
5. Se envía una orden de venta a mercado, el stop-loss se coloca a la mitad del ancho del canal sobre la entrada y el objetivo
   equivale a la línea media de M5.

### Operaciones largas

1. La pendiente horaria es positiva.
2. El mínimo de la última vela de 5 minutos toca o rompe el soporte de regresión de 5 minutos.
3. El mínimo de la última vela de 1 minuto toca o rompe el soporte de 1 minuto.
4. No hay posición larga existente abierta (`Position <= 0`).
5. Se envía una orden de compra a mercado, el stop-loss se coloca a la mitad del ancho del canal bajo la entrada y el objetivo
   equivale a la línea media de M5.

## Reglas de salida

- Los stops y objetivos se almacenan internamente y se evalúan en cada vela M1 completada. Si el rango de la vela cruza el nivel de
  stop almacenado, la posición se cierra de inmediato.
- Si el objetivo de beneficio se alcanza antes del stop, la posición también se cierra.
- El cierre restablece los niveles rastreados para que se pueda evaluar una señal nueva sin demora.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Degree` | 1 | Orden polinómico del canal de regresión (1=lineal, 2=parabólico, 3=cúbico). |
| `StdMultiplier` | 2.0 | Multiplicador de la desviación estándar que define el ancho de banda. |
| `Bars` | 250 | Número de velas usadas para el ajuste de regresión y el retroceso de pendiente. |
| `Shift` | 0 | Desplazamiento horizontal del punto de evaluación de regresión (limitado entre 0 y `Bars - 1`). |
| `UseTrading` | true | Deshabilita toda la generación de órdenes cuando se establece en `false`, mientras el canal continúa actualizándose. |

## Notas adicionales

- La estrategia almacena los niveles de stop y objetivo localmente porque las órdenes de mercado de StockSharp no adjuntan
  automáticamente niveles SL/TP.
- Funciona con cualquier instrumento que admita velas de minutos y horas; sin embargo, la lógica original fue diseñada para pares
  de forex.
- Ajusta `Bars` para que coincida con la volatilidad del instrumento negociado. Un valor menor reacciona más rápido, uno mayor
  produce canales más suaves.
- Establece `Degree` en 1 para un canal de regresión lineal recto (el más cercano a la versión lineal clásica), o usa grados más
  altos para emular los modos polinómicos del indicador MQL.
