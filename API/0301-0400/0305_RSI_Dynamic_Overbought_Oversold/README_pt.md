# Estratégia RSI com Sobrecompra/Sobrevenda Dinâmica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia extremos do RSI com limites adaptativos e filtro de tendência do preço, usando velas concluídas de 5 minutos. Calcula `R = RSI(14)`, `P = SMA(50)` do fechamento e `A = SMA(50)`, `D = StandardDeviation(50)` de R. Limites: `upper = min(A + 2 × D, 85)`, `lower = max(A − 2 × D, 15)`. Todos os indicadores devem estar formados.

Sem posição, compra com `R <= lower && Close >= P × 0.995` e vende com `R >= upper && Close <= P × 1.005`. Fecha toda a compra com `R >= A || Close < P × 0.995`, e toda a venda com `R <= A || Close > P × 1.005`. Cada ordem de sinal inicia pausa de `CooldownBars = 48` velas concluídas para novas decisões.

Posições executadas têm proteção nativa local percentual `StopLossPercent = 2` desde o preço real da entrada. Monitora bid/ask e envia saída a mercado independentemente da pausa dos sinais; exige cotações entre velas. Sem take-profit.

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
