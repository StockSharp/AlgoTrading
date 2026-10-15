# Estrategia de Estructura de Plazos en Materias Primas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera la pendiente de las curvas de futuros de materias primas. Compra contratos en backwardation y vende los que están en contango, apostando por la reversión a la media en la estructura de plazos.

Cada mes el sistema clasifica los futuros por carry, tomando posiciones largas en la mayor backwardation y cortas en el contango más pronunciado. Las posiciones se renuevan antes del vencimiento.

## Detalles

- **Datos**: Precios de futuros próximos y diferidos.
- **Entrada**: Largo en materias primas de mayor carry, corto en las de menor carry.
- **Salida**: Renovar al vencimiento del contrato o si el carry cambia de signo.
- **Instrumentos**: Futuros de materias primas.
- **Riesgo**: Ponderación equitativa en dólares con stop ante cambio adverso del carry.

