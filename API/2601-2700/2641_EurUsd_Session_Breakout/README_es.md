# Estrategia de Ruptura de Sesión EurUsd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la clásica idea de ruptura del EUR/USD donde un estrecho rango de la mañana europea se utiliza como trampolín para la sesión estadounidense. Monitorea una ventana deslizante de 24 velas (velas de 15 minutos por defecto) para medir el rango pre-US de trading, filtra los días donde el rango supera un umbral configurable de pips, y luego opera rupturas que ocurren completamente fuera de esa banda. Solo se permite un intento largo y uno corto por día de trading.

## Cómo Funciona

1. **Seguimiento de sesión** – al inicio de la hora de sesión US configurada, la estrategia bloquea el rango EU capturado por las 24 velas completadas más recientes (excluyendo la barra actual). El rango se ajusta automáticamente a valores de pips para cotizaciones de forex de 3 o 5 dígitos.
2. **Filtro de rango** – el trading se habilita solo si el rango EU capturado es menor que el umbral *Sesión EU Pequeña (pips)*.
3. **Validación de ruptura** – durante las horas de sesión US permitidas, y solo entre `(hora inicio EU + 5)` y `(hora inicio EU + 10)`, la estrategia busca velas cuyo cuerpo completo haya operado fuera del rango almacenado con un buffer adicional medido en puntos.
4. **Ejecución de órdenes** – se envía una compra a mercado cuando el mínimo de la barra permanece por encima de la parte superior del rango más el buffer. Se envía una venta a mercado cuando el máximo de la barra permanece por debajo de la parte inferior del rango menos el buffer. Los trades largos y cortos son indicadores independientes por lo que cada dirección puede intentarse una vez por día.
5. **Gestión de riesgo** – los niveles de stop loss y take profit se definen en pips, se convierten a distancias de precio absolutas, y se rastrean en cada vela finalizada usando extremos de máximo/mínimo.

## Parámetros

- **Inicio Sesión EU / Inicio Sesión US / Fin Sesión US** – horas (0–23) que definen cuándo comienza el monitoreo EU y cuándo está abierta la ventana de ruptura US.
- **Sesión EU Pequeña (pips)** – tamaño máximo del rango EU que aún permite trading.
- **Operar en Lunes** – habilita o deshabilita el trading los lunes, bloqueando los fines de semana.
- **Stop Loss (pips)** – distancia entre el precio de entrada y el stop protector, escalada automáticamente por tamaño de tick y dígitos.
- **Take Profit (pips)** – distancia del objetivo de beneficio, manejada de la misma forma que el stop.
- **Buffer de Ruptura (puntos)** – número de pasos de precio añadidos al disparador de ruptura para que la barra confirmadora deba estar completamente más allá del rango almacenado.
- **Tipo de Vela** – tipo de dato para la suscripción de velas; por defecto marco temporal de 15 minutos porque el script original fue diseñado para gráficos M15.

## Notas Adicionales

- La estrategia asume cuentas de compensación: los niveles protectores aplanan toda la posición usando órdenes de mercado.
- El estado diario se reinicia a medianoche para que el rango y las banderas de ruptura no se filtren entre sesiones, mientras que las posiciones abiertas retienen sus objetivos de precio.
- Dado que los niveles de stop-loss y take-profit se simulan con extremos de velas, los picos intrabarra que no aparecen en barras históricas no serán detectados.
