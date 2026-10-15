# Estrategia McGinley Dynamic (Mejorado)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el indicador "McGinley Dynamic (Improved)" de John R. McGinley, Jr. y opera cuando el precio de cierre cruza la línea dinámica. La estrategia soporta fórmulas moderna, original y con coeficiente personalizado, y puede mostrar opcionalmente la variante no restringida para comparación.

## Detalles

- **Entrada Largo**: el cierre cruza por encima de McGinley Dynamic.
- **Entrada Corto**: el cierre cruza por debajo de McGinley Dynamic.
- **Indicadores**: McGinley Dynamic, Unconstrained McGinley Dynamic opcional, EMA como referencia.
- **Valores predeterminados**: Period = 14, Formula = Modern, Custom k = 0.5, Exponent = 4.
- **Dirección**: Ambos.
