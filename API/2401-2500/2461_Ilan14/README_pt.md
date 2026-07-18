# Estratégia Ilan14
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Ilan14 é uma estratégia de grade de hedging que abre posições compradas e vendidas simultaneamente. Quando o mercado se move contra um lado por uma distância em pips definida pelo usuário, a estratégia adiciona uma nova ordem nessa direção com seu volume multiplicado pelo **Lot Exponent**. O preço médio da posição é rastreado e, uma vez que o preço reverta pela distância de **Take Profit** configurada, todas as ordens desse lado são fechadas.

Parâmetros:
- **Pip Step** – distância em pips entre as ordens da grade.
- **Lot Exponent** – multiplicador aplicado ao volume de cada ordem adicional.
- **Max Trades** – número máximo de ordens por direção.
- **Take Profit** – alvo de lucro em pips a partir do preço médio ponderado.
- **Initial Volume** – volume da primeira ordem.
- **Candle Type** – período para a assinatura de velas.

A implementação usa a API de alto nível do StockSharp com assinaturas de velas e evita coleções de dados manuais. Ambos os lados da grade são gerenciados de forma independente, permitindo que a estratégia capture rebounds após movimentos adversos.
