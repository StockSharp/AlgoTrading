# Estratégia Keltner com Filtro Sazonal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia rompimentos do canal Keltner filtrados por retornos mensais históricos. Nas velas concluídas de 5 minutos: `E = EMA(20)`, `A = ATR(14)`, `M = AtrMultiplier = 2`, `U = E + M × A`, `L = E − M × A`; `SeasonalThreshold = 0.5` ponto percentual. Aguarda indicadores formados e permissão para negociar.

Compra com `S > SeasonalThreshold`, `Close > U`, `Position <= 0`; vende com `S < -SeasonalThreshold`, `Close < L`, `Position >= 0`. `Volume + abs(Position)` reverte toda posição contrária. Guarda stop em `Close − M × A` para compras ou `Close + M × A` para vendas, pelo fechamento de sinal.

Primeiro verifica o stop a cada vela concluída: fecha compra com `Close <= stop` ou venda com `Close >= stop`, limpa o nível e encerra a vela. Caso contrário, verifica entradas e depois fecha compra abaixo de E ou venda acima de E. É um stop manual no fechamento, baseado no fechamento de sinal; sem stop de bolsa intravela, take-profit ou trailing.

Exige `SeasonalityHistoryFile`: CSV UTF-8 com abertura e fechamento mensais e cabeçalho `SecurityId,Month,Open,Close`. `Month` usa `yyyy-MM`, números usam ponto decimal, preços devem ser positivos e cada mês deve ser único para o instrumento. Outros identificadores são ignorados. Dados ausentes, linhas inválidas ou falta de observações correspondentes impedem iniciar.

Em cada vela de sinal, `S` é a média de `100 × (Close / Open − 1)` para o mesmo mês em anos anteriores, dentro de `SeasonalityLookbackYears = 10`. São necessárias `MinimumSeasonalSamples = 2` observações. O mês atual e os futuros são excluídos mesmo quando constam do arquivo. Sem histórico suficiente não há novas posições. Forneça preços mensais reais do instrumento; não há tabela fixa de pontuações.
