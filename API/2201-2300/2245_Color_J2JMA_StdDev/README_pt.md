# Estratégia Color J2JMA StdDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula a inclinação de uma Média Móvel Jurik (JMA) e a compara com o desvio padrão das inclinações recentes. A ideia é capturar movimentos direcionais fortes quando a inclinação supera um múltiplo da sua volatilidade recente.

Uma nova posição comprada é aberta quando a inclinação do JMA sobe acima do limiar alto (K2 × desvio padrão). Uma nova posição vendida é aberta quando a inclinação cai abaixo do limiar alto negativo. As posições existentes são fechadas quando a inclinação cruza o limiar baixo oposto (K1 × desvio padrão). Os níveis de stop loss e take profit são aplicados em pontos a partir do preço de entrada.

Parâmetros:
- **JMA Length** – período da média móvel Jurik.
- **StdDev Period** – número de inclinações recentes usadas para o desvio padrão.
- **K1** – multiplicador para o limiar baixo usado para fechar posições.
- **K2** – multiplicador para o limiar alto usado para abrir posições.
- **Candle Type** – período das velas para os cálculos.
- **Stop Loss** – stop de proteção em pontos.
- **Take Profit** – objetivo de lucro em pontos.
