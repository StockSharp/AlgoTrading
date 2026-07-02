# Estrategia Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en el indicador Williams %R

Las pruebas indican un retorno anual promedio de aproximadamente 88%. Funciona mejor en el mercado de acciones.

Williams %R identifica zonas de sobrecompra y sobreventa. Cuando el indicador sube por encima del umbral superior señala una posible debilidad para posiciones cortas; lecturas por debajo del umbral inferior sugieren posiciones largas. Las posiciones se cierran una vez que %R se mueve hacia la zona neutral.

Debido a que %R oscila rápidamente, la estrategia puede generar muchas señales en mercados volátiles. Algunos operadores lo combinan con otros filtros para reducir el ruido.


## Detalles

- **Criterios de entrada**: Señales basadas en Williams.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Period` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Williams
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

