# Estrategia New Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia New Martin replica el asesor experto de MetaTrader "New Martin" ejecutando una cobertura martingala simétrica en ambos lados del mercado. La estrategia mantiene una posición larga e inicial corta abiertas en todo momento y reequilibra la cobertura cuando las medias móviles suavizadas rápida y lenta (SMMA) se cruzan. Cuando un lado de la cobertura está perdiendo, el algoritmo multiplica la exposición en ese lado y simultáneamente realiza las ganancias en la pierna más rentable. Las salidas de toma de ganancias reciclan la cobertura reabriendo el lado faltante y opcionalmente purgando tanto los mejores como los peores rendidores para mantener la rejilla compacta.

La implementación apunta a la API de alto nivel de StockSharp y espera un portafolio con soporte de cobertura para que las piernas larga y corta puedan coexistir. Las órdenes se envían como órdenes de mercado por simplicidad, reflejando la lógica MQL original donde se asume que los llenados son inmediatos.

## Indicadores y Señales
- **SMMA rápida (longitud predeterminada 5):** rastrea la dirección de precio a corto plazo.
- **SMMA lenta (longitud predeterminada 20):** representa la tendencia dominante.
- **Detección de cruce:** un cruce de las dos barras completadas anteriores activa la adición martingala en la pierna de peor rendimiento. La señal se limita a una vez por vela almacenando el tiempo de apertura de la vela del último cruce.

## Gestión de Posiciones
- **Cobertura inicial:** tan pronto como se forman los indicadores, la estrategia abre una posición larga y una corta con el volumen inicial configurado. Ambas operaciones usan una distancia de toma de ganancias simétrica en pips.
- **Reciclaje de toma de ganancias:** cuando el precio toca el nivel de toma de ganancias de cualquier pierna, la estrategia cierra esa posición, registra el evento y opcionalmente cierra tanto las posiciones más rentables como las más perdedoras para realizar ganancias y pérdidas en pares. Los lados faltantes se reabren inmediatamente con el volumen base para que la cobertura permanezca equilibrada.
- **Promediación martingala:** en cada cruce de SMMA, el algoritmo identifica la posición con el menor beneficio no realizado. Aumenta la exposición en ese lado multiplicando el volumen de la operación por el multiplicador martingala (predeterminado 1.6) después de ajustar al paso de volumen del instrumento. La posición abierta más rentable se cierra justo después de la operación de promediación para liberar el beneficio bloqueado.

## Gestión de Riesgos
- **Protección contra caída de capital:** se rastrea el capital de portafolio más alto observado. Si la caída desde ese pico supera el porcentaje configurado, todas las posiciones abiertas se liquidan y la inicialización de la cobertura se pospone hasta la siguiente vela.
- **Volumen base dinámico:** cuando el capital crece al menos por el multiplicador martingala relativo al balance previamente registrado, el volumen de cobertura base se aumenta por el mismo multiplicador (respetando también los límites de volumen del exchange). Esto replica el comportamiento del EA original donde las ganancias se reinvierten para escalar la rejilla.
- **Normalización de volumen:** cada volumen solicitado se redondea hacia abajo al paso de volumen del exchange y se limita entre el volumen mínimo y máximo del instrumento para evitar rechazos de órdenes.

## Parámetros
- **Take Profit (pips):** distancia desde el precio de entrada para colocar el objetivo de toma de ganancias para cada pierna. Predeterminado 50 pips.
- **Initial Volume:** volumen base por lado de la cobertura. Predeterminado 0.1 contratos.
- **Slow MA / Fast MA:** longitudes de los indicadores SMMA lento y rápido (predeterminados 20 y 5). El período lento debe permanecer mayor que el período rápido.
- **Equity DD %:** caída máxima permitida desde el pico de capital antes de que todas las posiciones se cierren. Predeterminado 12%.
- **Multiplier:** factor martingala usado para promediar hacia abajo y para escalar el volumen base después de un crecimiento de capital importante. Predeterminado 1.6.
- **Candle Type:** marco temporal de las velas usadas para los cálculos. Predeterminado velas de 15 minutos, pero puede cambiarse para coincidir con el marco temporal del gráfico del EA original.

## Notas
- La estrategia requiere cuentas habilitadas para cobertura porque mantiene posiciones largas y cortas abiertas simultáneamente.
- Se usan órdenes de mercado para entradas y salidas, al igual que el experto MQL que dependía de llenados instantáneos. Adapte la lógica de órdenes si se necesita control de deslizamiento.
- Asegúrese de que los metadatos del instrumento (paso de precio, paso de volumen, volumen mínimo/máximo) estén correctamente configurados para que la normalización de volumen funcione como se espera.
