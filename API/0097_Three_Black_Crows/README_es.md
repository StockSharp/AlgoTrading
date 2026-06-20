# Three Black Crows Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tres Cuervos Negros es el equivalente bajista de Tres Soldados Blancos, compuesto por tres velas largas bajistas después de un avance. El patrón sugiere que los vendedores han tomado el control, ya que cada cierre cae cerca del mínimo de la sesión.

Las pruebas indican un rendimiento anual promedio de aproximadamente 178%. Funciona mejor en el mercado de acciones.

Esta estrategia inicia una posición corta una vez que aparece el tercer cuervo, esperando que el impulso continúe a la baja. También puede usarse para salir de posiciones largas abiertas por otros sistemas si el patrón se forma en resistencia.

El riesgo se gestiona con un stop porcentual ajustado por encima del máximo del patrón, y las operaciones se cierran si el precio vuelve a cerrar por encima de ese nivel.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
