# VWAP Williams R Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia VWAP Williams %R se centra en la reversión intradía alrededor del Precio Promedio Ponderado por Volumen. Observa cuándo el precio se aleja del VWAP mientras el oscilador Williams %R alcanza territorio de sobrecompra o sobreventa. La suposición es que las lecturas extremas cerca del VWAP a menudo conducen a un retroceso hacia la media.

Las pruebas indican un rendimiento anual promedio de aproximadamente 40%. Funciona mejor en el mercado cripto.

Cuando el oscilador cae por debajo de -80 y el precio opera bajo el VWAP, el escenario implica que la presión vendedora se está desvaneciendo y puede seguir un rebote. A la inversa, una lectura por encima de -20 mientras el precio está posicionado sobre el VWAP advierte que los compradores están agotados y es probable una corrección. La estrategia abre operaciones en la dirección de un posible retorno al VWAP y observa que ese movimiento se complete.

Este enfoque se adapta a los operadores intradía activos que prefieren oportunidades frecuentes de reversión a la media. Un stop‑loss pequeño relativo al VWAP mantiene el riesgo contenido mientras permite suficiente espacio para que el precio fluctúe antes de revertir.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price < VWAP && Williams %R < -80 (sobreventa bajo VWAP)
  - **Corto**: Price > VWAP && Williams %R > -20 (sobrecompra sobre VWAP)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio rompe por encima del VWAP
  - **Corto**: Salir de la posición corta cuando el precio rompe por debajo del VWAP
- **Stops**: Sí.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: VWAP Williams R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

