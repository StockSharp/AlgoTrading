# Estrategia VWAP Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia VWAP Williams %R se centra en la reversión intradía alrededor del Precio Promedio Ponderado por Volumen. Observa cuándo el precio se aleja del VWAP mientras el oscilador Williams %R alcanza territorio de sobrecompra o sobreventa. La suposición es que las lecturas extremas cerca del VWAP a menudo conducen a un retroceso hacia la media.

Cuando el oscilador cae por debajo de -80 y el precio opera bajo el VWAP, el escenario implica que la presión vendedora se está desvaneciendo y puede seguir un rebote. A la inversa, una lectura por encima de -20 mientras el precio está posicionado sobre el VWAP advierte que los compradores están agotados y es probable una corrección. La estrategia abre operaciones en la dirección de un posible retorno al VWAP y observa que ese movimiento se complete.

Este enfoque busca reversiones intradía a la media. Un stop-loss porcentual medido desde cada ejecución limita el movimiento adverso, mientras que la salida en el VWAP completa el retorno previsto a la media.

## Detalles
- **Criterios de entrada**:
  - **Largo**: el cierre está al menos un 0,1% por debajo del VWAP diario y Williams %R cruza -80 hacia abajo.
  - **Corto**: el cierre está al menos un 0,1% por encima del VWAP diario y Williams %R cruza -20 hacia arriba.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio rompe por encima del VWAP
  - **Corto**: Salir de la posición corta cuando el precio rompe por debajo del VWAP
- **Stops**: Sí.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `CooldownBars` = 60
  - `StopLossPercent` = 2%
  - `CandleType` = TimeSpan.FromMinutes(30)
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

