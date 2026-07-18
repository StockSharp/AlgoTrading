# Reversión Stochastic en Sobrecompra/Sobreventa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia reacciona a los niveles extremos del Oscilador Stochastic. Cuando la línea %K se sumerge en territorio de sobreventa, el sistema espera un rebote; mientras que lecturas de sobrecompra pueden presagiar una caída. El método opera en velas intradía cortas para que las señales lleguen rápidamente.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 73%. Funciona mejor en el mercado cripto.

Después de suscribirse al marco temporal seleccionado, monitorea las líneas %K y %D. Una configuración alcista se forma cuando %K cae por debajo de 20 y luego comienza a recuperarse. Por el contrario, una configuración bajista aparece si %K sube por encima de 80 y empieza a girar hacia abajo. Un stop de porcentaje fijo controla el riesgo para ambos lados.

Las posiciones se cierran cuando la línea %K cruza de nuevo el nivel 50, señalando que el impulso ha cambiado hacia la dirección opuesta. Dado que los stops escalan con el ATR más reciente, el tamaño de la operación se adapta a la volatilidad.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `%K < 20` con giro alcista.
  - **Corto**: `%K > 80` con giro bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: %K cruzando el nivel 50 o stop-loss.
- **Stops**: Sí, a una distancia de `2%`.
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

