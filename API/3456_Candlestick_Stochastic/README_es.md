# Vela japonesa Stochastic Estrategia de confirmación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el Expert Advisor de MetaTrader **Expert_CP_Stoch** dentro del nivel alto de StockSharp API. Combina patrones de reversión de velas japonesas con un filtro de oscilador estocástico %D para confirmar entradas y salidas de tiempo. El sistema escanea cada vela completa, mira hacia atrás tres barras para detectar formaciones alcistas o bajistas y requiere que la línea de señal estocástica esté en una zona de sobreventa o sobrecompra antes de abrir operaciones. Las posiciones se cierran cada vez que aparece el patrón opuesto o cuando la línea estocástica cruza un límite de salida configurable.

La configuración predeterminada refleja el experto original: %K período 33, %D período 37, desaceleración 30, sobreventa en 30, sobrecompra en 70 y niveles de cruce de salida en 20/80. Debido a que el oscilador estocástico de StockSharp utiliza datos altos/bajos/cerrados, el comportamiento corresponde a la configuración original de STO_LOWHIGH. El reconocimiento de patrones de velas se basa en los últimos doce cuerpos (de forma predeterminada) para calcular el tamaño promedio de vela utilizado en el filtrado de patrones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Se detecta uno de los patrones alcistas (Tres soldados blancos, Línea perforadora, Doji matutino, Envolvente alcista, Harami alcista, Estrella de la mañana, Líneas de encuentro alcistas) **y** el valor estocástico %D en la barra previamente cerrada está por debajo del umbral de sobreventa (predeterminado 30).
  - **Corto**: Se detecta uno de los patrones bajistas (Tres cuervos negros, Cubierta de nubes oscuras, Doji vespertino, Envolvente bajista, Harami bajista, Estrella vespertina, Líneas de encuentro bajista) **y** el valor estocástico %D en la barra previamente cerrada está por encima del umbral de sobrecompra (predeterminado 70).
- **Criterios de salida**:
  - **Largo**: Salga inmediatamente cuando aparezca un patrón bajista o cuando %D cruce por debajo del límite de salida superior (predeterminado 80) o por debajo del límite inferior (predeterminado 20).
  - **Corto**: Salga inmediatamente cuando aparezca un patrón alcista o cuando %D cruce por encima del límite de salida inferior (predeterminado 20) o por encima del límite superior (predeterminado 80).
- **Largo/Corto**: opera en ambas direcciones con reglas simétricas.
- **Stops**: Sin stop-loss/objetivo fijo; las salidas se basan en cambios de patrón y cruces estocásticos. Puede agregar protección de cartera en el iniciador si es necesario.
- **Valores predeterminados**:
  - `Body Average Period` = 12 velas utilizadas para calcular el tamaño del cuerpo de referencia para la calificación del patrón.
  - `Stochastic %K` = 33, `Stochastic %D` = 37, `Stochastic Smoothing` = 30.
  - `Oversold Threshold` = 30, `Overbought Threshold` = 70.
  - `Lower Exit Level` = 20, `Upper Exit Level` = 80.
- **Filtros**:
  - Categoría: Reconocimiento de patrones + confirmación de oscilador.
  - Dirección: Larga y corta.
  - Indicadores: oscilador Stochastic, múltiples patrones de velas.
  - Paradas: Solo salidas de patrón/oscilador (sin parada/objetivo mecánico).
  - Complejidad: Alta (detección de patrones de múltiples condiciones con promedios históricos).
  - Plazo: Funciona en cualquier plazo; por defecto son velas por hora.
  - Estacionalidad: Ninguna.
  - Redes neuronales: No.
  - Divergencia: No hay divergencia explícita; confirmación a través de niveles del oscilador.
  - Nivel de riesgo: Medio-alto por ausencia de paradas bruscas.

## Cómo funciona

1. Se suscribe a la serie de velas seleccionada y vincula un oscilador estocástico (%K, %D, desaceleración).
2. Mantiene las últimas tres velas completadas y los promedios móviles de los cuerpos/cierres de las velas para replicar la lógica de la biblioteca de patrones de MetaTrader.
3. Evalúa grupos de patrones alcistas/bajistas en cada vela terminada. Cada patrón sigue estrictamente las definiciones matemáticas originales (verificaciones corporales promedio, relaciones de puntos medios, requisitos de espacio, etc.).
4. Recupera los valores estocásticos de %D de las dos velas anteriores para detectar cruces y condiciones de sobreventa/sobrecompra.
5. Abre o cierra posiciones de mercado utilizando los métodos `BuyMarket`/`SellMarket` de alto nivel de StockSharp cuando las condiciones del patrón y del oscilador se alinean.
6. Opcionalmente, puede habilitar módulos de riesgo externos (por ejemplo, `StartProtection`) desde el iniciador si necesita una gestión de stop-loss.

## Notas prácticas

- Asegúrese de alimentar la estrategia con al menos `Body Average Period + 3` velas históricas antes de esperar señales; de lo contrario, las comprobaciones de patrones devolverán falso porque el cuerpo promedio no está definido.
- El filtro estocástico utiliza el valor %D de la vela **anterior**, replicando la forma en que la señal de MetaTrader evaluó `StochSignal(1)`.
- Debido a que el reconocimiento del patrón de velas es sensible a las brechas y los tamaños relativos de las velas, los resultados pueden variar en instrumentos con poca liquidez o datos sintéticos.
- Para acelerar la optimización, puede ajustar los umbrales de sobreventa/sobrecompra y los períodos estocásticos manteniendo intactas las definiciones de las velas.
- Si necesita un comportamiento de STO_CLOSECLOSE (cierre/estocástico de cierre), reemplace el oscilador de StockSharp por uno configurado para cálculos de solo cierre en una mejora futura.
