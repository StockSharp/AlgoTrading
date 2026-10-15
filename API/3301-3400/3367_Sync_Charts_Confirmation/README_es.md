# Estrategia de confirmación de gráficos de sincronización
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia refleja la idea de la utilidad original MQL "SyncCharts" al monitorear dos transmisiones de velas del mismo instrumento y
tomar decisiones comerciales solo cuando ambas corrientes confirman la misma dirección de tendencia. La serie maestra actúa como cuadro de referencia.
(el que un comerciante normalmente ve), mientras que la serie de seguidores representa un gráfico auxiliar (por ejemplo, un marco de tiempo más rápido o
una agregación alternativa). Al obligar a ambas corrientes a ponerse de acuerdo antes de ingresar al mercado, el sistema filtra el ruido proveniente de
Desincronización temporal entre intervalos de gráficos.

La configuración funciona mejor en instrumentos que exhiben una estructura de tendencia de múltiples períodos de tiempo, como futuros sobre índices y pares de divisas líquidos.
Debido a que ambos gráficos deben moverse juntos antes de realizar una operación, las señales falsas se reducen y la estrategia naturalmente limita
exposición durante fases caóticas del mercado cuando los plazos no coinciden o se imprimen nuevas velas en diferentes momentos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Tanto el promedio móvil simple (SMA) maestro como el seguidor tienen pendiente ascendente en sus velas terminadas más recientes, y
las marcas de tiempo de esas velas difieren en menos que la tolerancia de sincronización.
  - **Corto**: Ambos SMA tienen pendiente descendente y la diferencia de marca de tiempo está dentro de la ventana de tolerancia.
- **Criterios de salida**:
  - Desincronización horaria: si las últimas velas están separadas por más de la tolerancia permitida, la posición se aplana.
  - Discrepancia de tendencia: si un SMA sube mientras el otro baja, la posición abierta se cierra inmediatamente.
- **Paradas**: la lógica de aplanamiento implícita actúa como una parada suave. No se envía ninguna parada forzosa por separado.
- **Largo/Corto**: Se intercambian ambas partes.
- **Valores predeterminados**:
  - Vela maestra: tiempo de 5 minutos.
  - Vela seguidora: período de 1 minuto.
  - Duración de SMA: 20 períodos en ambas transmisiones.
  - Tolerancia de sincronización: 15 segundos entre tiempos de apertura de velas.
- **Filtros**:
  - Categoría: Confirmación de tendencia / multiplazo.
  - Dirección: Bidireccional.
  - Indicadores: SMA (doble flujo).
  - Paradas: sin parada fija, se aplana automáticamente cuando los gráficos divergen.
  - Complejidad: Media (multisuscripción con comprobaciones de sincronización).
  - Plazo: Configurable (intradiario predeterminado).
  - Estacionalidad: Ninguna.
  - Redes neuronales: No.
  - Divergencia: utiliza la divergencia del marco temporal como filtro (requiere acuerdo, no divergencia de precios).
  - Nivel de riesgo: Moderado debido al requisito de confirmación.

## como funciona

1. Se crean dos suscripciones de velas a través del StockSharp API de alto nivel: una para el gráfico maestro y otra para el seguidor.
2. Cada feed es procesado por un SMA con la misma longitud, lo que genera un indicador de dirección de tendencia (`up` si el valor de SMA aumenta frente al
vela anterior, `down` en caso contrario).
3. Cada vez que ambas velas terminan, la estrategia verifica que sus marcas de tiempo estén lo suficientemente cerca (diferencia absoluta por debajo del
tolerancia configurada).
4. Si los gráficos están sincronizados y ambas tendencias apuntan hacia arriba, la estrategia compra (cerrando cualquier corto primero). Si ambas tendencias apuntan hacia abajo,
vende en corto (cerrando cualquier posición larga primero).
5. Cualquier pérdida de sincronización o desacuerdo de tendencia desencadena un aplanamiento inmediato para mantener la cuenta alineada con los gráficos del
relojes comerciante.

## Uso recomendado

- Aplicar al mismo instrumento en dos períodos de tiempo diferentes que normalmente se correlacionan (por ejemplo, 5 minutos y 1 minuto, o cada hora y
15 minutos).
- Aumente la tolerancia de sincronización si trabaja con fuentes de datos exóticas que imprimen velas con retrasos menores.
- Combínelo con un administrador de riesgos externo o un módulo de parada adicional cuando lo implemente en operaciones reales.
