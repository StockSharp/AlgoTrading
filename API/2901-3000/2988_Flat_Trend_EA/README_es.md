# Estrategia Flat Trend EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Flat Trend EA Strategy es un puerto de StockSharp del asesor experto MQL5 "Flat Trend EA". El algoritmo combina el indicador Parabolic SAR con el Índice de Dirección Promedio (ADX) para detectar cuatro estados de mercado: tendencia alcista, tendencia bajista, fin de compra y fin de venta. La estrategia reacciona solo a velas completadas del marco temporal configurado y refleja la lógica original de cerrar posiciones opuestas antes de abrir una nueva.

## Lógica de trading
- **Señal de compra**: el punto del Parabolic SAR se imprime por debajo del precio de cierre y la línea +DI del ADX está por encima de la línea -DI. Cualquier exposición corta se cierra inmediatamente, y se abre un nuevo largo cuando no hay posición activa.
- **Señal de venta**: el punto del Parabolic SAR se imprime por encima del precio de cierre y +DI es menor o igual a -DI. Cualquier exposición larga se cierra antes de abrir una operación corta.
- **Filtros de fin de tendencia**: cuando el SAR está por encima del precio mientras +DI es mayor que -DI, la estrategia marca el fin de una tendencia corta; cuando el SAR está por debajo del precio mientras +DI es menor o igual a -DI, marca el fin de una tendencia larga. Ambos eventos fuerzan el cierre de las posiciones existentes sin abrir una nueva operación.
- **Ventana de trading**: filtros de sesión opcionales restringen las entradas al intervalo `[StartHour, EndHour)`. Las señales fuera de la sesión aún pueden cerrar operaciones, pero las nuevas entradas se omiten.

## Gestión de riesgos
- Las distancias de **stop-loss y take-profit** se miden en pips (escaladas automáticamente para instrumentos de tres y cinco dígitos). Los precios se normalizan al paso del instrumento.
- El **trailing stop** se activa después de que la posición gana más que `TrailingStopPips + TrailingStepPips`. Las posiciones largas siguen por debajo del último cierre, los cortos por encima. Cuando el trailing está deshabilitado, el nivel de stop permanece fijo.
- **Salidas de protección**: en cada vela finalizada la estrategia comprueba los precios bajos/altos contra los niveles de stop-loss, take-profit y trailing. Cualquier brecha cierra la posición y restablece el seguimiento de riesgo.

## Parámetros
- `StopLossPips` – distancia al stop de protección en pips.
- `TakeProfitPips` – distancia al objetivo en pips.
- `TrailingStopPips` – distancia de trailing stop en pips (establecer en 0 para deshabilitar el trailing).
- `TrailingStepPips` – progreso adicional requerido antes de que el trailing stop se mueva; debe ser positivo cuando el trailing está habilitado.
- `UseTradingHours` – habilita el filtro de ventana de trading.
- `StartHour` / `EndHour` – hora de inicio inclusiva y hora de fin exclusiva para entradas (hora del exchange).
- `AdxPeriod` – período de suavizado para el ADX, que controla la sensibilidad de +DI y -DI.
- `SarStart`, `SarIncrement`, `SarMaximum` – configuraciones de aceleración del Parabolic SAR que coinciden con el indicador original (0.02 / 0.02 / 0.2 por defecto).
- `CandleType` – marco temporal utilizado para las suscripciones de velas y los cálculos del indicador.
- `Volume` – heredado de `Strategy`, representa el tamaño de la orden utilizado al entrar en nuevas posiciones.

## Indicadores
- **Índice de Dirección Promedio (ADX)** proporciona los componentes +DI y -DI utilizados para determinar la dirección actual de la tendencia.
- **Parabolic SAR** define si la estructura del mercado es alcista o bajista y proporciona el nivel del punto para la lógica de trailing.

## Notas adicionales
- El tamaño del pip se calcula a partir de la configuración del instrumento: para instrumentos con tres y cinco decimales, el paso de precio se multiplica por diez para coincidir con la definición MQL de un pip.
- La estrategia siempre cierra las posiciones existentes cuando aparecen señales opuestas o de fin antes de evaluar nuevas entradas, reproduciendo el flujo de trabajo original del EA.
- Solo se proporciona la implementación en C#; no se crea ninguna versión ni carpeta de Python, según lo solicitado.
