# Estratégia McGinley Dynamic (Melhorado)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o indicador "McGinley Dynamic (Improved)" de John R. McGinley, Jr. e opera quando o preço de fechamento cruza a linha dinâmica. A estratégia suporta fórmulas moderna, original e com coeficiente personalizado, e pode exibir opcionalmente a variante irrestrita para comparação.

## Detalhes

- **Entrada Comprado**: fechamento cruza acima do McGinley Dynamic.
- **Entrada Vendido**: fechamento cruza abaixo do McGinley Dynamic.
- **Indicadores**: McGinley Dynamic, Unconstrained McGinley Dynamic opcional, EMA como referência.
- **Valores padrão**: Period = 14, Formula = Modern, Custom k = 0.5, Exponent = 4.
- **Direção**: Ambos.
