# Estrategia ColorX2MA Digit NN3 MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Recrea el Asesor Experto de triple marco temporal basado en el indicador ColorX2MA Digit.
- Utiliza un indicador de media móvil de doble suavizado personalizado que imita la lógica X2MA original con métodos de suavizado seleccionables (Simple, Exponential, Smoothed, Linear Weighted, Jurik, Kaufman Adaptive).
- Aplica tres instancias de indicador independientes (12h, 6h, 3h por defecto); cada instancia puede abrir o cerrar exposición larga/corta de forma independiente según su propia configuración.
- Agrega el volumen deseado de cada marco temporal y opera la diferencia con órdenes de mercado para que la posición neta siempre coincida con la suma de señales individuales.
- Las señales se confirman después de `SignalBars` barras consecutivas con la misma dirección de pendiente, lo que emula el desplazamiento `SignalBar` en la versión MQL.
- Incluye interruptores opcionales para permitir o prohibir la apertura/cierre de exposición larga y corta por separado para cada marco temporal, reproduciendo las banderas "Must Trade" del original.

## Parámetros
- **A/B/C Candle Type** – tipo de datos (marco temporal) para cada instancia de indicador.
- **Fast/Slow Method** – método de suavizado para la primera y segunda media móvil dentro del clon X2MA.
- **Fast/Slow Length** – período de las respectivas medias móviles (por defecto: 12 y 5).
- **Signal Bars** – número de barras consecutivas requeridas antes de aceptar una nueva dirección (por defecto: 1).
- **Digits** – precisión de redondeo aplicada a la salida del indicador antes del cálculo de pendiente (simula la entrada `Digit`).
- **Price Type** – fuente de precio usada por el indicador (cierre, apertura, mediana, típico, ponderado, simplificado, cuarto, fórmulas TrendFollow y DeMark).
- **Allow Long/Short Entry/Exit** – banderas booleanas que controlan si un marco temporal específico puede abrir o cerrar exposición larga/corta.
- **Volume** – volumen operado contribuido por el marco temporal cuando está largo (positivo) o corto (negativo).

## Señales y gestión de posición
1. Cada marco temporal procesa únicamente velas completadas y actualiza su valor de indicador.
2. Cuando la pendiente de la media doblemente suavizada se vuelve positiva (índice de color 0 en el indicador MQL) y permanece así durante el número configurado de barras, el contexto se vuelve alcista:
   - La exposición corta existente se cierra si `Allow Short Exit` está habilitado.
   - Se abre una posición larga del volumen configurado si `Allow Long Entry` está habilitado.
3. Cuando la pendiente se vuelve negativa (índice de color 2), el contexto se vuelve bajista:
   - La exposición larga existente se cierra si `Allow Long Exit` está habilitado.
   - Se abre una posición corta del volumen configurado si `Allow Short Entry` está habilitado.
4. La estrategia suma los volúmenes deseados de los tres marcos temporales y envía una orden de mercado por la diferencia con el portafolio actual para que el `Position` global siempre refleje la intención combinada.

## Notas
- Los tipos de suavizado no admitidos de la biblioteca MQL (JurX, Parabolic MA, T3, variaciones VIDYA/AMA) no están expuestos; si se requieren pueden mapearse manualmente.
- El indicador personalizado redondea valores usando `Digits` y trabaja solo en velas completadas, evitando el redibujado intra-barra.
- No se agrega stop-loss o take-profit incorporado porque el original usa gestión monetaria MMRec; los parámetros `Volume` permiten el dimensionamiento manual en su lugar.
