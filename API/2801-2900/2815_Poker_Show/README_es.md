# Estrategia Poker Show
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Poker Show es un porteo directo del asesor experto de MetaTrader 5 "Poker_SHOW". Combina un filtro de tendencia de media móvil con un disparador probabilístico que imita el reparto de una mano de póker. Las operaciones se ejecutan solo cuando el valor de la mano generada aleatoriamente cae por debajo de un umbral configurable de combinación de póker. El enfoque produce entradas poco frecuentes mientras permanece alineado con la tendencia predominante detectada por la media móvil.

La estrategia trabaja en un único símbolo y se basa en velas basadas en tiempo regular. Las decisiones de trading se evalúan una vez por vela completada, lo que coincide con el asesor original que reacciona en la apertura de cada nueva barra.

## Lógica principal

1. **Filtro de tendencia de media móvil**
   - Se calcula una media móvil configurable (SMA, EMA, SMMA o LWMA) a partir de la fuente de precio seleccionada (cierre, apertura, máximo, mínimo, mediana, típico o precio ponderado).
   - El indicador puede desplazarse hacia adelante en el tiempo para reproducir el input "shift" de MetaTrader. La estrategia siempre usa el valor de la última vela completamente formada, igual que el EA fuente.

2. **Puerta de probabilidad**
   - Cada lado (largo o corto) extrae un valor aleatorio independiente entre 0 y 32.767 en cada barra.
   - El sorteo se compara con la combinación de póker seleccionada. Las combinaciones de rango más alto (p. ej., escalera de color) tienen umbrales numéricos más pequeños y por lo tanto se activan con menor frecuencia, mientras que las de rango inferior (p. ej., una pareja) operan con más frecuencia.

3. **Reglas direccionales**
   - Las operaciones largas requieren que la media móvil se mantenga por encima del precio al menos la distancia configurada. Cuando la opción **Señales invertidas** está activada, la condición se invierte.
   - Las operaciones cortas requieren que la media móvil se mantenga por debajo del precio con el mismo margen, con la condición invertida cuando el interruptor de inversión está activo.
   - Solo puede haber una posición activa a la vez. Entrar en la dirección opuesta compensa automáticamente cualquier exposición abierta antes de establecer la nueva operación.

4. **Gestión de riesgo**
   - Los niveles opcionales de stop loss y take profit se calculan en pasos de precio (puntos) relativos al precio de ejecución. Establecer una distancia en cero deshabilita el nivel correspondiente.
   - Los stops y objetivos se verifican en cada vela completada. Cuando se alcanzan, la estrategia cierra la posición y restablece los marcadores de riesgo.

5. **Protección de posición**
   - El módulo de protección integrado de StockSharp se activa al inicio para preservar la cuenta de pérdidas inesperadas durante ejecuciones manuales.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Combinación de póker** | Umbral de probabilidad que debe superar el sorteo aleatorio para permitir un nuevo trade. Representa manos clásicas de póker, desde escalera de color (más rara) hasta una pareja (más común). |
| **Volumen** | Volumen de orden en lotes. Usado tanto para entradas nuevas como para invertir posiciones existentes. |
| **Stop loss** | Distancia entre el precio de entrada y el stop protector, medida en pasos de precio. Poner en cero para deshabilitar. |
| **Take profit** | Distancia entre el precio de entrada y el objetivo de beneficio, medida en pasos de precio. Poner en cero para deshabilitar. |
| **Habilitar compra** | Permite a la estrategia abrir posiciones largas. |
| **Habilitar venta** | Permite a la estrategia abrir posiciones cortas. |
| **Distancia MA** | Distancia mínima en pasos de precio entre el valor de la media móvil y el precio actual. Actúa como filtro de confirmación de tendencia. |
| **Período MA** | Número de barras usadas por la media móvil. |
| **Desplazamiento MA** | Desplazamiento horizontal aplicado a la media móvil (en barras), coincidiendo con el input `ma_shift` de MetaTrader. |
| **Método MA** | Tipo de suavizado de la media móvil: simple, exponencial, suavizada o ponderada lineal. |
| **Precio aplicado** | Precio de vela utilizado en el cálculo de la media móvil. |
| **Señales invertidas** | Invierte la comparación entre la media móvil y el precio, intercambiando efectivamente la lógica de largo y corto. |
| **Tipo de vela** | Marco temporal de la suscripción de velas. El predeterminado es una hora para replicar la configuración original. |

## Notas y recomendaciones

- La puerta de probabilidad hace que la estrategia sea altamente estocástica. Los backtests deben usar múltiples ejecuciones o análisis de Monte Carlo para comprender la distribución de los resultados.
- Debido a que la gestión de operaciones depende de velas completadas, los picos intrabarra grandes pueden sobrepasar los niveles de stop o objetivo antes de que la estrategia pueda reaccionar. Considere ejecutar en marcos temporales más bajos si este comportamiento no es deseable.
- Para reproducir fielmente el entorno de MetaTrader, asegúrese de que el instrumento use el mismo tamaño de contrato y paso de precio para que las distancias basadas en puntos coincidan con los lotes y valores de pip originales.
- La estrategia utiliza órdenes de mercado (`BuyMarket` y `SellMarket`) como en el asesor experto fuente. El manejo del deslizamiento se delega a la infraestructura de ejecución de StockSharp.
