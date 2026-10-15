# Estrategia DCA Simulation para CryptoCommunity
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia simula el promedio de costo en dólares con órdenes de seguridad opcionales y un take-profit con trailing. Comienza con una orden base y puede invertir capital adicional periódicamente o promediar hacia abajo tras caídas de precio.

## Detalles

- **Criterios de entrada**:
  - Cuando no hay posición abierta y la fecha está dentro del rango configurado, comprar una cantidad base.
  - Órdenes DCA periódicas opcionales cada N velas.
  - Órdenes de seguridad opcionales cuando el precio cae un porcentaje especificado desde el máximo reciente.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Take profit en un porcentaje objetivo, opcionalmente con trailing stop.
- **Stops**: Take profit / trailing stop.
- **Valores predeterminados**:
  - Orden base = 100 USD.
  - Monto DCA = 10 USD cada 30 velas.
  - Monto de orden de seguridad = 100 USD con 15% de desviación de precio.
  - Take profit = 1000%, trailing = 25%.
  - Fecha de inicio = 2021-11-01, fecha de fin = 9999-01-01.
- **Filtros**:
  - Categoría: Acumulación.
  - Dirección: Largo.
  - Indicadores: Ninguno.
  - Stops: Sí.
  - Complejidad: Moderado.
  - Marco temporal: Cualquiera.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
