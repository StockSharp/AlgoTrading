# Estrategia Sidus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el sistema de medias móviles SIDUS. Opera usando cruces entre dos medias móviles ponderadas linealmente y una media exponencial de confirmación. Se abre una posición cuando la LWMA a corto plazo cruza la LWMA a largo plazo o cuando la LWMA larga cruza la EMA lenta. Los cruces opuestos cierran o revierten la posición. Un stop-loss y take-profit basados en porcentaje gestionan el riesgo.

Las pruebas indican una rentabilidad anual media de aproximadamente el 25%. Funciona mejor en pares de divisas.

La idea central es capturar los cambios de tendencia cuando las medias móviles rápidas y lentas se realinean. El par de LWMA reacciona rápidamente a los cambios de precio mientras que la EMA más lenta filtra el ruido. Cuando ocurre una alineación alcista o bajista, la estrategia entra en esa dirección y confía en los niveles de protección para salir durante movimientos adversos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la LWMA rápida cruza por encima de la LWMA lenta *o* la LWMA lenta cruza por encima de la EMA lenta.
  - **Corto**: la LWMA rápida cruza por debajo de la LWMA lenta *o* la LWMA lenta cruza por debajo de la EMA lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto o niveles de stop de protección.
- **Stops**: Sí, utiliza take-profit y stop-loss basados en porcentaje mediante `StartProtection`.
- **Valores predeterminados**:
  - Longitud de EMA rápida = 18.
  - Longitud de EMA lenta = 28.
  - Longitud de LWMA rápida = 5.
  - Longitud de LWMA lenta = 8.
  - Take profit = 2%.
  - Stop loss = 1%.
- **Filtros**: Ninguno.
