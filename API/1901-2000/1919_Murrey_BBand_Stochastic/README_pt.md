# Estratégia Murrey Math com BBand e Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões nas linhas extremas do Murrey Math usando Bandas de Bollinger e um Oscilador Estocástico como confirmação.

O método calcula os níveis de Murrey a partir dos preços mais altos e mais baixos durante um período configurável. Quando o preço se aproxima da linha 0/8 durante condições de sobrevenda, a estratégia compra. Quando o preço se aproxima da linha 8/8 durante condições de sobrecompra, vende. Um filtro de largura mínima das Bandas de Bollinger impede a negociação em mercados laterais.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: O fechamento está dentro da *Entry Margin* acima da linha 0/8, Estocástico <= 21 e largura das Bandas de Bollinger >= limiar.
  - **Vendido**: O fechamento está dentro da *Entry Margin* abaixo da linha 8/8, Estocástico >= 79 e largura das Bandas de Bollinger >= limiar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**
  - Posições compradas fecham na linha 1/8 ou se o preço cair abaixo da linha -2/8.
  - Posições vendidas fecham na linha 7/8 ou se o preço subir acima da linha +2/8.
- **Stops**: As linhas de Murrey (-2/8 ou +2/8) atuam como stops de proteção.
- **Filtros**
  - Filtro de largura das Bandas de Bollinger.
  - Filtro do oscilador estocástico.
