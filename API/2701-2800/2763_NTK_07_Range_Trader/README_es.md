# Estrategia NTK 07 de Operación en Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia NTK 07 de Operación en Rango es un port del asesor experto de MetaTrader "NTK 07". El algoritmo mantiene órdenes stop simétricas alrededor del precio de mercado actual y gestiona posiciones abiertas con lógica de trailing y take-profit configurables. El objetivo es capturar rupturas que ocurren cerca de los bordes o el centro de un rango de precio a corto plazo respetando controles de riesgo estrictos.

## Ideas centrales

- **Disparadores de entrada** – Cuando la estrategia está plana evalúa un rango de lookback configurable. Si el precio está en los bordes del rango o cerca de su punto medio (dependiendo del modo de trade seleccionado) coloca órdenes buy stop y sell stop en un offset definido en pasos de precio.
- **Conciencia del rango** – Los precios más altos y más bajos de las últimas *N* velas terminadas definen el rango de trading. Una longitud cero desactiva el filtro y permite colocar órdenes inmediatamente.
- **Riesgo adaptativo** – Cada entrada usa el volumen base mientras un multiplicador de lotes opcional puede piramidizar órdenes stop adicionales después de que se abre una posición. Un límite de volumen a nivel de cartera bloquea nuevas órdenes cuando la exposición superaría el tope.
- **Gestión de salida** – Tan pronto como se llena una posición, se cancela la orden stop opuesta. La estrategia luego registra órdenes de stop protector y take-profit opcionales usando los offsets configurados. El trailing puede seguir el máximo/mínimo de la vela anterior, una media móvil, o un buffer de distancia fija.
- **Filtro de sesión** – El trading se permite solo entre las horas de inicio y fin seleccionadas y se desactiva automáticamente los fines de semana.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Entry Volume** | Tamaño base para cada orden de entrada. |
| **Total Volume Limit** | Tamaño máximo de posición acumulada. Un valor de `0` deshabilita el tope. |
| **Net Step** | Distancia en pasos de precio entre el mercado y las órdenes stop de entrada. |
| **Stop Loss** | Offset inicial del stop-loss en pasos de precio relativo al precio de entrada. |
| **Take Profit** | Distancia del take-profit en pasos de precio. Establecer en `0` para deshabilitar objetivos de ganancia. |
| **Trailing Stop** | Distancia en pasos de precio usada para la lógica de trailing. |
| **Lot Multiplier** | Multiplicador aplicado al piramidizar en una posición existente. |
| **Trail High/Low** | Si está habilitado, los stops protectores siguen los extremos de la vela anterior. |
| **Trail Moving Average** | Habilita el trailing usando un valor de media móvil. Solo un modo de trailing puede estar activo. |
| **Trading Start/End Hour** | Ventana de tiempo de plataforma inclusiva para el trading. |
| **Range Bars** | Número de velas completadas usadas para calcular el rango de trading. `0` deshabilita el filtro. |
| **Trade Mode** | `EdgesOfRange` requiere que el precio toque los bordes del rango, `CenterOfRange` espera hasta que el precio esté cerca del punto medio del rango. |
| **MA Period** | Longitud de la media móvil usada para el trailing. |
| **Candle Type** | Agregación de velas usada para todos los cálculos. |

## Flujo de trabajo

1. **Suscripción de datos** – La estrategia se suscribe a la serie de velas configurada y calcula la media móvil así como el precio más alto y más bajo sobre la longitud de rango elegida.
2. **Estado plano** – Mientras no hay posición abierta, la estrategia evalúa la condición del rango. Si se satisface, coloca órdenes buy stop y sell stop pareadas en el offset especificado respetando el límite de volumen global.
3. **Manejo de posición** – Cuando una entrada se llena, el stop opuesto se cancela. La estrategia coloca inmediatamente órdenes de stop-loss protector y take-profit opcional. La lógica de trailing luego actualiza el stop protector en cada nueva vela terminada.
4. **Piramidización** – Si el multiplicador de lotes es mayor que `1`, se coloca una orden stop adicional en la dirección de la posición actual mientras el límite de volumen total lo permita.
5. **Salida** – Los stops o take-profits aplanan la posición y cancelan las órdenes protectoras restantes. El sistema luego vuelve a monitorear la próxima interacción del rango.

## Notas

- La estrategia funciona enteramente con pasos de precio, lo que la hace adecuada para instrumentos con diferentes tamaños de tick.
- El trading se desactiva automáticamente los sábados y domingos para reflejar el comportamiento de la implementación MQL original.
- Solo un modo de trailing puede habilitarse a la vez; habilitar ambos desencadenará un error de configuración al inicio.
