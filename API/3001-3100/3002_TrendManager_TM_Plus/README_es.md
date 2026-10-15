# Estrategia TrendManager TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
TrendManager TM Plus es una estrategia de seguimiento de tendencia convertida del asesor experto original de MetaTrader 5 `Exp_TrendManager_Tm_Plus.mq5`. La estrategia se basa en el indicador personalizado TrendManager, que compara dos medias móviles suavizadas y resalta la distancia entre ellas. Cuando la distancia supera un umbral configurable, la estrategia abre posiciones en la dirección de la tendencia prevalente y cierra posiciones cuando la tendencia se revierte o cuando se activan reglas de protección.

## Lógica de negociación
1. Construir dos medias móviles en la serie de velas seleccionada. Los métodos de suavizado y las longitudes de ambas líneas son configurables.
2. Calcular la distancia entre las medias rápida y lenta. Si la distancia es mayor o igual al umbral, el indicador reporta una tendencia alcista. Si la distancia es menor o igual al umbral negativo, el indicador reporta una tendencia bajista. De lo contrario, no hay señal accionable.
3. Almacenar los estados de color (0 para tendencia alcista, 1 para tendencia bajista, 3 para neutral) en un breve historial. El parámetro `SignalBar` selecciona cuántas barras cerradas hacia atrás se evalúan, siguiendo la lógica MQL original.
4. Cuando aparece un nuevo color de tendencia alcista, la estrategia opcionalmente cierra posiciones cortas existentes y puede abrir una posición larga si las entradas largas están permitidas. Por el contrario, un nuevo color de tendencia bajista puede cerrar largos y abrir cortos.
5. Las salidas opcionales basadas en tiempo y precio cierran operaciones abiertas cuando el tiempo de mantenimiento excede `MaxPositionAge`, cuando el precio cae por debajo de `StopLossDistance` para largos (o por encima para cortos), o cuando se alcanza `TakeProfitDistance`.

## Parámetros
- **Candle Type** – marco temporal utilizado para la generación de señales (predeterminado: velas de 4 horas para coincidir con el script original).
- **Fast MA Method / Slow MA Method** – algoritmos de suavizado para las líneas rápida y lenta. Opciones disponibles: Simple, Exponential, Smoothed, Weighted, Jurik y Kaufman Adaptive.
- **Fast Length / Slow Length** – períodos para las medias móviles.
- **Distance Threshold (`DvLimit`)** – distancia absoluta mínima entre las medias rápida y lenta requerida para detectar una tendencia. Convertir valores en puntos de MT5 a unidades de precio (p.ej., 70 puntos en un símbolo de 5 dígitos ≈ 0.00070).
- **Signal Bar** – número de barras cerradas hacia atrás usadas para confirmar una señal nueva. Un valor de 1 reproduce el comportamiento predeterminado de la estrategia MQL.
- **Allow Long Entries / Allow Short Entries** – habilitar o deshabilitar entradas para cada dirección.
- **Close Long / Close Short on Opposite Signal** – cerrar inmediatamente posiciones abiertas cuando aparece una señal del color opuesto.
- **Use Time Exit / Max Position Age** – habilitar y configurar el tiempo máximo de mantenimiento antes de que una posición sea cerrada forzosamente.
- **Order Volume** – volumen fijo enviado con órdenes de mercado. Este parámetro reemplaza la configuración de gestión de dinero de la versión MetaTrader.
- **Stop Loss Distance / Take Profit Distance** – desplazamientos de precio de protección opcionales medidos en unidades de precio absoluto (establecer en cero para deshabilitar).

## Notas de implementación
- Se utilizan indicadores de StockSharp para reproducir el comportamiento de TrendManager. Los modos de suavizado exóticos no admitidos de la biblioteca original recurren a la media móvil de StockSharp disponible más cercana.
- El procesamiento de señales mantiene un pequeño búfer de historial para que la comprobación de `SignalBar` pueda detectar transiciones igual que el asesor MT5.
- Las salidas de protección se evalúan en velas completadas. Los rellenos intrabarra del entorno original se aproximan comparando los máximos y mínimos de las velas con las distancias configuradas.
- Los parámetros específicos de MT5 como `Deviation` y el dimensionamiento de posición basado en margen se han reemplazado con equivalentes compatibles con StockSharp.

## Recomendaciones de uso
1. Elegir un tipo de vela que coincida con el horizonte de negociación previsto. H4 se mantiene como predeterminado para paridad con el código fuente.
2. Calibrar el umbral a la volatilidad del instrumento. Los instrumentos con ticks o volatilidad mayores requieren valores más altos.
3. Combinar la salida temporal con las distancias de stop-loss y take-profit para emular los controles de riesgo del asesor original.
4. Para activos que cotizan en ambas direcciones, mantener ambas palancas de entrada habilitadas para que la estrategia pueda revertir posiciones cuando cambie la tendencia.

## Diferencias respecto al asesor experto original
- El dimensionamiento de órdenes usa un `OrderVolume` fijo en lugar del módulo de gestión de dinero de MT5.
- Las órdenes de stop-loss y take-profit se simulan usando datos de velas en lugar de la colocación inmediata de órdenes MT5.
- La estrategia usa las medias móviles nativas de StockSharp. Algunas opciones de suavizado (p.ej., Jurik, Kaufman adaptive) se mapean directamente, mientras que variantes MT5 no admitidas vuelven a la coincidencia más cercana.
- Las salidas basadas en tiempo dependen de `MaxPositionAge` con precisión `TimeSpan` en lugar de contadores de minutos sin procesar.

Este documento proporciona la información esencial requerida para configurar, ejecutar y extender la estrategia TrendManager TM Plus dentro del ecosistema StockSharp.
