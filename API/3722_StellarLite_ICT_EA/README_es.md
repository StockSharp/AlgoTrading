# Estrategia TIC StellarLite EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
StellarLite ICT EA es un algoritmo de estilo discrecional que traduce el manual de estrategia de la empresa "Stellar Lite" a StockSharp. La estrategia fusiona dos modelos de entrada de Inner Circle Trader (ICT), Silver Bullet y el modelo 2022, y automatiza el plan parcial de obtención de beneficios utilizado en el asesor experto original MetaTrader. Funciona con cualquier instrumento que proporcione información sobre velas, pasos de precio y pasos de volumen.

## Flujo de trabajo principal
1. **Sesgo direccional del marco temporal superior**: un promedio móvil en el marco temporal superior seleccionado debe inclinarse en la dirección comercial y el precio debe cerrar más allá del promedio. Sólo después de que se confirme el sesgo se evaluará la lógica del marco temporal más bajo.
2. **Confirmación de barrido de liquidez**: la estrategia monitorea una ventana retrospectiva configurable y busca rupturas de máximos o mínimos recientes. Silver Bullet requiere un barrido en la dirección comercial, mientras que el modelo 2022 requiere un barrido de incentivos en la dirección opuesta.
3. **Cambio de estructura de mercado (MSS)**: las últimas tres velas terminadas deben confirmar un cambio: un cierre más alto por encima del máximo anterior para operaciones largas o un cierre más bajo por debajo del mínimo anterior para operaciones cortas.
4. **Detección de brecha de valor razonable (FVG)**: la estrategia escanea las diez velas más recientes en busca de desequilibrios alcistas o bajistas creados por velas de desplazamiento. La entrada solo se permite cuando el cierre actual está dentro del espacio detectado.
5. **Filtro NDOG / NWOG** – la vela actual debe ser una barra de rango estrecho. Su rango alto-bajo no puede exceder `AtrThreshold` multiplicado por el valor `AverageTrueRange`.
6. **Entrada, parada y objetivos**: el precio de entrada se coloca en el medio de la brecha o en el retroceso OTE (entrada comercial óptima) definido por el parámetro de relación Fibonacci. La parada protectora se ubica más allá de la reciente oscilación de la liquidez, y se proyectan tres niveles de toma de ganancias utilizando los ratios riesgo-recompensa configurados.
7. **Gestión comercial**: la posición se dimensiona según el porcentaje de riesgo seleccionado o vuelve al volumen de la estrategia. Cuando se alcanzan TP1, TP2 y TP3, la estrategia cierra el 50%, 25% y 25% de la posición de forma predeterminada, mueve el stop al punto de equilibrio después de TP1 (con un desplazamiento opcional), activa un trailing stop después de TP2 y liquida el resto en TP3 o al alcanzar un stop.

## Parámetros
- **Vela de entrada (`CandleType`)**: velas de período de tiempo más bajo utilizadas para señales de entrada.
- **Período de tiempo más alto (`HigherTimeframeType`)**: velas que alimentan el promedio móvil de sesgo.
- **Período MA superior (`HigherMaPeriod`)**: duración media móvil para la detección de sesgos.
- **ATR Período (`AtrPeriod`)**: búsqueda retrospectiva del filtro de consolidación ATR.
- **Retrospectiva de liquidez (`LiquidityLookback`)**: número de velas inspeccionadas para localizar fondos de liquidez.
- **ATR Umbral (`AtrThreshold`)**: rango de vela máximo permitido como fracción de ATR.
- **Recompensa de riesgo TP1/TP2/TP3 (`Tp1Ratio`, `Tp2Ratio`, `Tp3Ratio`)**: multiplicadores de riesgo-recompensa para objetivos.
- **% de cierre de TP1/TP2/TP3 (`Tp1Percent`, `Tp2Percent`, `Tp3Percent`)** – porcentajes de cierre parcial.
- ** Punto de equilibrio después de TP1 (`MoveToBreakEven`)**: alterna el ajuste del punto de equilibrio.
- **Compensación de equilibrio (`BreakEvenOffset`)**: número de pasos de precio agregados o restados al mover el stop.
- **Distancia de seguimiento (`TrailingDistance`)** – distancia de trailing stop (en pasos de precio) activada después de TP2.
- **Utilice Silver Bullet/Utilice el modelo 2022 (`UseSilverBullet`, `Use2022Model`)**: habilite o deshabilite cada configuración.
- **Utilice entrada OTE (`UseOteEntry`)**: calcule la entrada dentro de la zona óptima de entrada comercial.
- **% de riesgo (`RiskPercent`)**: porcentaje de capital arriesgado por operación para derivar el tamaño de la posición.
- **OTE Inferior (`OteLowerLevel`)** – coeficiente Fibonacci para el nivel OTE.

## Notas prácticas
- La estrategia requiere velas terminadas; Asegúrese de que la fuente de datos proporcione precios cercanos y pasos de volumen.
- El tamaño de la posición vuelve al parámetro de estrategia `Volume` cuando el valor de la cartera o la información del valor del tick no están disponibles.
- La detección de liquidez y la lógica MSS se basan en el caché del historial más reciente (20 velas de forma predeterminada); Permita que la estrategia recopile suficientes datos antes de esperar señales.
- Las salidas parciales respetan el paso de volumen del instrumento; si la fracción solicitada es menor que el volumen mínimo negociable se omite el cierre.
- La lógica de seguimiento sigue actualizando el stop sólo en la dirección de las ganancias y nunca afloja los controles de riesgo existentes.

## Archivos
- `CS/StellarLiteIctEaStrategy.cs` – implementación de la estrategia StockSharp.
- `README.md` – Documentación en inglés.
- `README_zh.md` – Documentación en chino simplificado.
- `README_ru.md` – Documentación rusa.
