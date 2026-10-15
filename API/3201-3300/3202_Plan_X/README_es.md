# Estrategia de Plan X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Plan X es un sistema de breakout convertido desde el asesor experto original de MetaTrader 5. Evalúa el cierre de cada vela finalizada contra una vela de referencia desplazada por un número configurable de barras. Cuando el cierre más reciente supera el cierre de referencia por una altura de canal especificada, la estrategia abre una posición en la dirección del breakout. La reversión de señal opcional permite operar breakouts en la dirección opuesta.

La implementación usa la API de alto nivel de StockSharp. Soporta stop-loss ajustable, take-profit, lógica de trailing stop y un filtro de sesión de trading.

## Cómo funciona

1. **Procesamiento de velas** – la estrategia se suscribe al tipo de vela configurado y procesa solo las velas finalizadas. Se mantiene un breve historial de cierres para comparar el último valor con una barra de referencia desplazada.
2. **Detección de breakout** – si el último cierre es mayor que el cierre de referencia por más que la altura del canal, se produce una señal long. Si es menor por la misma cantidad, se genera una señal short. Cuando el indicador de reversión está habilitado, las señales se invierten.
3. **Ejecución de órdenes** – la estrategia usa órdenes de mercado. Al revertir desde una posición opuesta, el volumen de la orden incluye automáticamente el valor absoluto de la posición actual para aplanar y re-entrar en una sola operación.
4. **Gestión de riesgos** – los niveles de stop-loss y take-profit se establecen inmediatamente después de la entrada. Un trailing stop puede reemplazar al stop original cuando el precio se mueve favorablemente por más que la distancia de trailing más el paso de trailing.
5. **Filtro de tiempo** – el trading puede limitarse a una hora de inicio y fin. Si la hora de inicio es mayor que la hora de fin, la ventana se trata como cruzando la medianoche.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Stop Loss (pips)` | Distancia del stop de protección en pips, convertida a unidades de precio basadas en el paso de precio del instrumento. |
| `Take Profit (pips)` | Distancia del objetivo en pips. |
| `Trailing Stop (pips)` | Distancia entre el precio y el trailing stop. Establecer en cero para deshabilitar el trailing. |
| `Trailing Step (pips)` | Beneficio adicional requerido antes de que el trailing stop avance. Debe ser positivo cuando el trailing está habilitado. |
| `Channel Height (pips)` | Umbral de breakout expresado en pips. |
| `Candle Shift` | Número de barras entre el último cierre y la vela de referencia. |
| `Use Time Control` | Habilita o deshabilita el filtro de sesión de trading. |
| `Start Hour` | Primera hora (0–23) cuando se permite el trading. |
| `End Hour` | Última hora (0–23) cuando se permite el trading. |
| `Reverse Signals` | Invierte la dirección del breakout. |
| `Order Volume` | Tamaño de la orden de mercado expresado en lotes/contratos. |
| `Candle Type` | Tipo de datos de velas utilizado para el análisis. |

## Lógica de señales

- **Entrada long** – último cierre ≥ cierre de referencia + altura del canal, reversión deshabilitada.
- **Entrada short** – último cierre ≤ cierre de referencia − altura del canal, reversión deshabilitada.
- Cuando la reversión está habilitada, la lógica intercambia las condiciones long y short.

## Lógica de trailing stop

- El trailing stop se activa cuando el movimiento favorable supera `Trailing Stop + Trailing Step` en términos de precio.
- Para posiciones long el stop se mueve a `high − Trailing Stop` si el nuevo valor es mayor que el stop existente.
- Para posiciones short el stop se mueve a `low + Trailing Stop` si el nuevo valor es menor que el stop existente.

## Notas adicionales

- El cálculo del tamaño de pip emula la versión MQL multiplicando el paso de precio por 10 para instrumentos de 3 o 5 decimales.
- El trading fuera de la sesión permitida omite nuevas entradas pero sigue gestionando posiciones abiertas.
- La estrategia llama a `StartProtection()` una vez durante el inicio para habilitar los servicios de protección de cartera integrados.
