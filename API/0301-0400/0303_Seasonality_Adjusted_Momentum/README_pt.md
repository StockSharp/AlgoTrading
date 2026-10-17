# Estratégia de Momentum Ajustado por Sazonalidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina momentum com retornos mensais históricos. Nas velas concluídas de 5 minutos, calcula `M = Momentum(14)` e `A = SMA(14)` de M. Sem posição, compra se `S >= SeasonalityThreshold` e `M > A`; vende se `S <= -SeasonalityThreshold` e `M < A`. O padrão `SeasonalityThreshold = 0.5` é medido em pontos percentuais.

Fecha compras se a condição sazonal deixar de valer ou `M < A`, e vendas se a condição falhar ou `M > A`. Cada ordem de sinal inicia uma pausa de `CooldownBars = 120` velas concluídas para novas decisões. As posições executadas têm proteção nativa local separada `StopLossPercent = 2` desde o preço real, monitorada por bid/ask e executada a mercado, sem esperar a pausa.

Exige `SeasonalityHistoryFile`: CSV UTF-8 com abertura e fechamento mensais e cabeçalho `SecurityId,Month,Open,Close`. `Month` usa `yyyy-MM`, números usam ponto decimal, preços devem ser positivos e cada mês deve ser único para o instrumento. Outros identificadores são ignorados. Dados ausentes, linhas inválidas ou falta de observações correspondentes impedem iniciar.

Em cada vela de sinal, `S` é a média de `100 × (Close / Open − 1)` para o mesmo mês em anos anteriores, dentro de `SeasonalityLookbackYears = 10`. São necessárias `MinimumSeasonalSamples = 2` observações. O mês atual e os futuros são excluídos mesmo quando constam do arquivo. Sem histórico suficiente não há novas posições. Forneça preços mensais reais do instrumento; não há tabela fixa de pontuações.
