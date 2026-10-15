# Estratégia VR Setka P2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma abordagem baseada em grade, traduzida do especialista MetaTrader 4 `VR---SETKAp2`.
Opera quando o fechamento diário desvia do máximo ou mínimo do dia em um percentual determinado.
A estratégia abre posições compradas após uma queda significativa a partir do máximo diário e
posições vendidas após uma alta significativa a partir do mínimo diário. Uma vez em posição,
sai a uma distância fixa de take profit. O volume pode opcionalmente aumentar usando um esquema simples de Martingale.

## Parâmetros
- **TakeProfit** – distância até o alvo de lucro em passos de preço.
- **Lot** – volume base para cada negociação.
- **Percent** – limiar percentual calculado a partir da variação diária.
- **UseMartingale** – habilita o aumento de volume ao adicionar a uma posição perdedora.
- **Slippage** – derrapagem de preço permitida para ordens.
- **Correlation** – deslocamento aplicado ao calcular os níveis da grade.
- **Candle Type** – período usado para os cálculos (diário por padrão).

## Lógica
1. Assinar velas diárias.
2. Para cada vela concluída, calcular os desvios percentuais em relação ao máximo e mínimo diários.
3. Entrar comprado ou vendido dependendo do desvio e da direção da vela anterior.
4. Fechar a posição quando o nível de take profit for atingido.

Esta implementação demonstra como um especialista de grade clássico do MetaTrader pode ser portado para a API de alto nível do StockSharp.
