# Estrategia CHO suavizada EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica la lógica del Asesor Experto original "CHO Smoothed EA". Observa los cruces del oscilador Chaikin en velas completadas y suaviza el oscilador con una media móvil configurable. Los filtros opcionales limitan las operaciones a una sesión específica, restringen la dirección de las operaciones y validan las señales con confirmación de línea cero. Cuando se acepta una señal, la estrategia envía una orden de mercado y gestiona la posición utilizando distancias fijas en puntos para protección de stop-loss, take-profit y trailing.

## Lógica de trading
- Los valores del Oscilador Chaikin se calculan en cada vela terminada con períodos rápidos y lentos configurables.
- Una media móvil del oscilador crea la línea de señal. El período y el tipo de media móvil se pueden ajustar.
- Las entradas largas se producen cuando el oscilador cruza por encima de la línea suavizada. Las entradas cortas ocurren en el cruce opuesto. Las señales se pueden revertir para operar en contra de la dirección original.
- Si el filtro de nivel cero está habilitado, ambos valores del oscilador deben estar por debajo de cero para operaciones largas y por encima de cero para operaciones cortas.
- La estrategia puede cerrar automáticamente posiciones opuestas antes de iniciar una nueva operación o ignorar las señales hasta que la posición actual sea plana. También puede imponer un modo de posición única.
- El comercio se puede restringir a una ventana de tiempo diaria. Se admiten ventanas que cruzan la medianoche.
- Después de una entrada, la estrategia almacena el precio de entrada, convierte las distancias de puntos configuradas en compensaciones de precios y monitorea las velas para eventos de stop-loss, take-profit y trailing-stop.

## Gestión del riesgo
- Los niveles de stop-loss y take-profit se calculan a partir del precio de entrada utilizando distancias entre puntos multiplicadas por el paso del precio del instrumento.
- El trailing stop se activa después de que el precio avanza en el paso de seguimiento configurado y luego sigue la distancia de seguimiento.
- Cuando se alcanza un nivel de protección, la posición se cierra inmediatamente con una orden de mercado y se restablecen todos los niveles de riesgo.

## Parámetros
- **Tipo de vela**: período de tiempo utilizado para crear las velas para los cálculos del indicador.
- **Período rápido / Período lento** – Períodos rápidos y lentos del oscilador Chaikin.
- **Período MA de señal/Tipo MA de señal**: suaviza los ajustes de media móvil aplicados al oscilador.
- **Usar nivel cero**: requiere que ambos valores de oscilador estén en el lado correcto de cero antes de operar.
- **Modo comercial**: permite solo direcciones largas, solo cortas o ambas direcciones.
- **Señales inversas**: intercambia entradas largas y cortas.
- **Cerrar opuesto**: cierre posiciones opuestas existentes antes de abrir una nueva operación.
- **Solo una posición**: evita entradas cuando una posición ya está abierta.
- **Usar control de tiempo/hora de inicio/hora de finalización**: habilite y configure la ventana de negociación diaria.
- **Stop Loss (pts)** – distancia en puntos para la parada de protección.
- **Take Profit (pts)** – distancia en puntos para objetivos de ganancias.
- **Trailing Stop (pts)** – distancia del trailing stop en puntos.
- **Trailing Step (pts)** – movimiento mínimo favorable (en puntos) antes de mover el trailing stop.

## Notas adicionales
- Establezca la propiedad `Volume` de la estrategia antes de iniciarla para controlar el tamaño de la operación.
- Debido a que la estrategia emite órdenes de mercado, garantiza suficiente liquidez y considera el deslizamiento en entornos reales.
- Cuando los tiempos de inicio y finalización de la ventana de negociación son iguales, la estrategia permanece inactiva, coincidiendo con el comportamiento original del Asesor Experto.
