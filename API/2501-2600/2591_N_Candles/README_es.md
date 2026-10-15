# Estrategia N Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia N Candles replica el asesor experto MQL que entra en una operación cuando un número configurable de velas consecutivas comparten la misma dirección. Una vez que las `N` velas completadas más recientes son todas alcistas, la estrategia envía una orden de compra de mercado. Cuando todas son bajistas, envía una orden de venta de mercado. No se incluye lógica de salida; la posición debe gestionarse externamente o mediante estrategias adicionales.

## Descripción general

- **Régimen de mercado**: Funciona mejor en mercados que exhiben ráfagas cortas de momentum.
- **Instrumentos**: Cualquier instrumento que soporte trading continuo (FX, futuros, cripto).
- **Marcos temporales**: Configurables; por defecto velas de 1 hora.
- **Tipos de órdenes**: Órdenes de mercado sin stops protectores ni objetivos.

## Cómo funciona

1. En cada vela completada la estrategia evalúa las últimas `N` velas.
2. Si cada vela en esa ventana es alcista, emite una orden de compra de mercado con el volumen configurado.
3. Si cada vela es bajista, emite una orden de venta de mercado.
4. Las velas doji (apertura igual al cierre) reinician el conteo y suprimen el trading hasta que se forme una nueva racha.
5. La estrategia no gestiona posiciones abiertas; las señales repetidas añaden a la dirección existente en cuentas de compensación neta.

## Parámetros

- **Consecutive Candles**: Número de velas idénticas requeridas antes de colocar una orden.
- **Volume**: Tamaño de la orden de mercado enviada en cada señal.
- **Candle Type**: Serie de velas usada para la detección de racha (marco temporal o tipo de vela personalizado).

## Notas de uso

- Dado que la estrategia carece de stops o salidas, combínela con gestión manual, estrategias protectoras o controles de riesgo de cartera.
- En mercados muy volátiles considere reducir el recuento de velas o el marco temporal para capturar rachas más rápidas.
- Rachas consecutivas excesivas pueden acumular posiciones grandes; monitoree el apalancamiento y los límites de cuenta.
