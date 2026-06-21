# Estratégia EMA mais WPR v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina o oscilador Williams %R com o filtro de tendência EMA. Opera quando o WPR atinge níveis extremos após uma retração. Inclui saídas opcionais baseadas em WPR, trailing stops e saída baseada em barras.

## Detalhes

- **Comprado**: WPR atinge -100 após retração e a tendência EMA é de alta.
- **Vendido**: WPR atinge 0 após retração e a tendência EMA é de baixa.
- **Indicadores**: Williams %R, EMA.
- **Stops**: stop-loss e take-profit fixos, trailing stop opcional.
