# Estrategia de Reverse Day Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Reverse Day Fractal es una estrategia de acción del precio que busca reversiones bruscas después de una ruptura intradía. El algoritmo analiza las últimas tres velas terminadas. Cuando la barra actual forma un nuevo extremo más allá de las dos velas anteriores y cierra de vuelta en la dirección opuesta, lo trata como una ruptura fallida y entra en una operación de reversión. Las órdenes protectoras se gestionan mediante distancias configurables de take-profit, stop-loss y trailing stop medidas en pasos de precio.

## Lógica de trading
- **Configuración alcista**:
  - La vela terminada actual establece un *mínimo más bajo* que cada una de las dos velas anteriores.
  - La vela cierra *por encima* de su precio de apertura, indicando un rechazo alcista del nuevo mínimo.
  - Cuando se cumplen estas condiciones y la estrategia puede operar, abre una posición larga. Opcionalmente puede cerrar primero un corto existente.
- **Configuración bajista**:
  - La vela terminada actual establece un *máximo más alto* que cada una de las dos velas anteriores.
  - La vela cierra *por debajo* de su precio de apertura, indicando un rechazo bajista del nuevo máximo.
  - Cuando se satisfacen estas condiciones, abre una posición corta, cerrando opcionalmente un largo existente primero.
- **Gestión de posición**: la estrategia puede configurarse para permitir solo una posición abierta a la vez (comportamiento predeterminado). Cuando está deshabilitado, revertirá una posición existente añadiendo el volumen requerido para cambiar la dirección.
- **Controles de riesgo**: al inicio, la estrategia llama a `StartProtection` para aplicar protecciones de take-profit, stop-loss y trailing stop usando las distancias de punto configuradas. Cuando un trailing stop está habilitado, el stop protector seguirá el precio en pasos discretos.

## Parámetros
- `Trade Volume` – volumen de orden para nuevas entradas.
- `Take Profit` – distancia al objetivo de beneficio medida en pasos de precio. Cero para deshabilitar.
- `Stop Loss` – distancia al stop protector medida en pasos de precio. Cero para deshabilitar.
- `Trailing Stop` – distancia de trailing stop en pasos de precio. Cero para deshabilitar.
- `Trailing Step` – movimiento mínimo (en pasos) requerido antes de ajustar el trailing stop.
- `Only One Position` – cuando está habilitado, la estrategia ignora nuevas señales mientras hay una posición abierta.
- `Candle Type` – tipo de datos de velas usado para los cálculos (predeterminado: marco temporal de 1 hora).

## Notas
- Las señales se generan solo en velas terminadas proporcionadas por la suscripción configurada.
- La estrategia mantiene en memoria los dos extremos de vela más recientes; por lo tanto, necesita al menos dos velas completadas después del inicio antes de poder generar una señal.
- Los valores de parámetros predeterminados replican el asesor experto MQL4 original: volumen de 0.01 lote, stop loss de 20 puntos, take profit de 10 puntos, trailing stop de 25 puntos y trailing step de 5 puntos.
