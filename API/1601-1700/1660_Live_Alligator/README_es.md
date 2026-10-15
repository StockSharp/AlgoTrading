# Estrategia Alligator en Vivo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones de tendencia usando una configuración dinámica del Alligator y varios filtros EMA.
Abre una nueva posición cuando las líneas del Alligator cambian de dirección y cinco EMAs confirman el movimiento.
Un filtro opcional de horario de trading limita las entradas a una sesión elegida.
La posición abierta se cierra cuando el precio cruza una media móvil suavizada trailing.

- **Criterios de entrada**
  - Alligator lips por encima de jaws con teeth por debajo de jaws y la barra anterior lips por debajo de jaws -> abrir largo después de una tendencia bajista.
  - Alligator lips por debajo de jaws con teeth por encima de jaws y la barra anterior lips por encima de jaws -> abrir corto después de una tendencia alcista.
  - Cinco EMAs sobre precios de cierre, ponderado, típico, mediano y de apertura deben estar estrictamente ordenadas en la dirección de la tendencia.
- **Criterios de salida**
  - El precio cruza la SMMA trailing basada en `TrailPeriod`.
  - Stop-loss opcional aplicado en la apertura de la operación.
- **Indicadores utilizados**
  - Medias Móviles Suavizadas para las líneas del Alligator y el stop trailing.
  - Medias Móviles Exponenciales en diferentes tipos de precio.

Los parámetros permiten configurar el período base del Alligator, período de confirmación EMA, período trailing, stop-loss y ventana de horario de trading.
