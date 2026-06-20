# Plantilla Base de Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta carpeta proporciona un andamio mínimo para construir ideas de trading personalizadas.
La estrategia solo calcula una media móvil exponencial y expone una amplia gama de
parámetros comunes: habilitación de operaciones largas o cortas, take profit y stop loss
opcionales, y rangos de optimización. Los desarrolladores pueden insertar su propia lógica
de entrada y salida dentro de los marcadores de posición para prototipar rápidamente
nuevos sistemas.

La plantilla también demuestra cómo iniciar el módulo de protección integrado con
objetivos basados en porcentajes, facilitando la experimentación con diferentes
configuraciones de riesgo. Dado que no se incluyen señales reales, este script no está
destinado a ser operado tal como está, sino más bien a servir como punto de partida para
investigación adicional.

## Detalles

- **Criterios de entrada**: No implementados – reemplazar con reglas personalizadas.
- **Largo/Corto**: Configurable mediante parámetros.
- **Criterios de salida**: No implementados – reemplazar con reglas personalizadas.
- **Stops**: Take profit y stop loss porcentuales opcionales gestionados por el módulo de protección.
- **Valores predeterminados**:
  - Longitud EMA = 10.
  - Take profit = 1.2%, Stop loss = 1.8% (deshabilitado por defecto).
- **Filtros**:
  - Categoría: Plantilla
  - Dirección: Configurable
  - Indicadores: EMA
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Definido por el usuario
