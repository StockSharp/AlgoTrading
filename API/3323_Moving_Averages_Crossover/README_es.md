# Estrategia Moving Averages
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia replica el asesor experto clásico de cruce de medias móviles de MQL. Usa APIs StockSharp de alto nivel para monitorizar dos medias móviles simples calculadas desde la serie de velas seleccionada. Las señales se generan cuando la media rápida cruza la lenta, y la estrategia puede cerrar opcionalmente una posición activa cuando ocurre el cruce opuesto.

## Lógica de negociación
- Suscribirse al tipo de vela configurado y calcular SMA rápida y lenta en cada vela completada.
- Detectar un cruce alcista cuando la SMA rápida pasa de estar por debajo a estar por encima de la SMA lenta. Si no hay posición activa, abrir un largo con el volumen especificado.
- Detectar un cruce bajista cuando la SMA rápida pasa de estar por encima a estar por debajo de la SMA lenta. Si no hay posición activa, abrir un corto con el volumen especificado.
- Opcionalmente cerrar de inmediato una posición existente cuando se detecta el cruce opuesto, replicando el interruptor "Close on Opposite Signal" del script original.

## Gestión de riesgo
- Aplicar stop loss y take profit fijos expresados en puntos de precio. Ambos niveles se recalculan para cada nueva entrada.
- Mover el stop protector a break-even después de que el precio recorra la distancia de activación configurada y mantener un desplazamiento adicional como beneficio bloqueado.
- Activar trailing stop cuando la posición gane la distancia inicial definida. El stop se desplaza usando el precio de vela más favorable y nunca se mueve contra la operación.

## Parámetros
- **Fast MA Period:** longitud de la SMA rápida usada para detectar cruces.
- **Slow MA Period:** longitud de la SMA lenta usada para detectar cruces.
- **Trade Volume:** tamaño de orden en lotes.
- **Stop Loss (points):** distancia en puntos del instrumento para el stop loss inicial.
- **Take Profit (points):** distancia en puntos del instrumento para el take profit inicial.
- **Break-even Trigger:** distancia de beneficio que activa mover el stop a break-even.
- **Break-even Offset:** puntos adicionales mantenidos como beneficio después de activar break-even.
- **Trailing Start:** distancia de beneficio requerida antes de habilitar trailing stop.
- **Trailing Distance:** distancia mantenida entre precio y trailing stop.
- **Close On Opposite:** si cerrar una operación activa cuando aparece un cruce opuesto.
- **Candle Type:** serie de velas usada para cálculos de indicadores.

## Notas de uso
- Asegúrese de que la estrategia esté conectada a un instrumento con `PriceStep` válido. Cuando no hay paso disponible, se usa un valor de 1.
- La gestión de trailing y break-even opera sobre velas completadas, igual que el EA original que reacciona al cierre de barra.
- Optimice las longitudes de medias móviles y los ajustes de riesgo para adaptar el sistema a distintos mercados o marcos temporales.
