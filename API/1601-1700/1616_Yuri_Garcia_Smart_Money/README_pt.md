# Estratégia de Dinheiro Inteligente de Yuri Garcia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de conceito de dinheiro inteligente busca reações de preço dentro de zonas de alto volume e áreas de suporte/resistência de quatro horas. Confirma as entradas com o delta acumulado e pullbacks de sombra, com o objetivo de seguir o fluxo de ordens institucionais.

Os testes indicam um retorno anual médio de cerca de 42%. Funciona melhor em BTC e nos principais índices.

O sistema calcula o stop loss e o take profit baseados em ATR com uma proporção risco/recompensa configurável. As negociações são permitidas compradas, vendidas ou ambas, e as posições são abertas somente quando o preço está dentro da zona, ocorre um pullback de sombra e o delta suporta o movimento.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço dentro da zona com buffer, pullback altista de sombra, delta acumulado em alta.
  - **Vendido**: Preço dentro da zona, pullback baixista de sombra, delta acumulado em queda.
- **Comprado/Vendido**: Configurável (ambos, somente compra ou somente venda).
- **Critérios de saída**:
  - Stop loss ou take profit baseados em ATR.
- **Stops**: Sim, baseados em ATR.
- **Filtros**:
  - Zona HTF, confirmação de delta acumulado, pullback de sombra.
