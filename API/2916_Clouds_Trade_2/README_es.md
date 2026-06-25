# Estrategia Clouds Trade 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port en C# del asesor experto "cloud's trade 2" de Vladimir Karputov. Opera rompimientos confirmados por dos fractales recientes de Bill Williams y un cruce de sobrecompra/sobreventa en el oscilador estocástico. La gestión de operaciones replica los inputs originales con stop loss, take profit, trailing stop y bloqueos mínimos de ganancia configurables.

## Lógica de trading

- **Datos**: velas de un solo marco temporal (predeterminado 15 minutos).
- **Indicadores**:
  - Oscilador estocástico usando el lookback %K configurado, el ralentizamiento y el suavizado %D.
  - Ventana deslizante de máximos/mínimos de cinco velas para reconstruir fractales superiores e inferiores.
- **Entrada**:
  - **Largo**: aparecen dos fractales inferiores consecutivos más recientemente que cualquier fractal superior **o** el %D estocástico cae por debajo de 20 mientras cruza por debajo de %K. No debe haber ninguna posición abierta y el filtro opcional de una operación por día debe permitir una nueva entrada.
  - **Corto**: dos fractales superiores consecutivos aparecen primero **o** el %D estocástico sube por encima de 80 mientras cruza por encima de %K.
- **Salidas y protección**:
  - Offsets estáticos de stop loss y take profit desde el precio de entrada.
  - Trailing stop opcional que se mueve solo cuando la ganancia actual excede la distancia de trailing configurada más el paso.
  - Cerrar posiciones una vez que se alcanza un objetivo de ganancia basado en dinero o en distancia de precio.
  - Los stops se emulan inspeccionando los máximos/mínimos de velas, replicando el comportamiento gestionado por el broker en la versión MQL.

## Parámetros

- **Order Volume**: tamaño de orden base para entradas.
- **Stop/Take Offsets**: distancias de precio absolutas; ajustar al valor de tick del instrumento para reproducir los inputs originales basados en pips.
- **Trailing Stop & Step**: offsets en unidades de precio que rigen cuándo se mueve el stop.
- **Min Profit (Currency / Points)**: cerrar operaciones una vez que la ganancia no realizada supera estos umbrales.
- **Use Fractals / Use Stochastic**: habilitar cualquiera de los componentes de señal de forma independiente.
- **One Trade Per Day**: evitar múltiples entradas durante la misma fecha de trading.
- **Stochastic Settings**: longitudes de lookback de %K, ralentizamiento de %K y suavizado de %D.
- **Candle Type**: marco temporal para la suscripción de velas de la estrategia.

## Notas

- Las verificaciones de ganancia de posición aproximan los ajustes originales de comisión/swap usando el movimiento del precio por el tamaño de la posición.
- La lógica de trailing sigue la implementación MQL al requerir que la ganancia supere la distancia de trailing más el paso antes de desplazar el stop.
- Para imitar los inputs predeterminados MQL basados en pips en pares Forex, establecer los offsets de stop/take al valor de pip deseado multiplicado por el valor del punto del instrumento (por ejemplo, 50 pips ≈ 0.005 para cotizaciones de cinco dígitos).
