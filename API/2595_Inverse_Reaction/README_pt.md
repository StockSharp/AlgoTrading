# Estratégia de Reação Inversa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Reação Inversa é um sistema de reversão à média inspirado no consultor especialista original do MetaTrader "IREA". Reage a movimentos de barra única incomumente grandes e antecipa uma reação inversa na próxima barra. A estratégia calcula um nível de confiança dinâmico a partir de ranges de candles recentes e só opera quando os movimentos de preço excedem esse nível mas permanecem dentro dos limites definidos pelo usuário. Apenas uma posição pode estar aberta a qualquer momento.

## Lógica de trading
1. **Indicador de Reação Inversa** – Para cada candle concluído a estratégia mede a variação abertura/fechamento e alimenta seu valor absoluto em uma média móvel simples de comprimento `MaPeriod`. A variação média é multiplicada por `Coefficient` para formar um limiar dinâmico semelhante ao Nível de Confiança Dinâmico (DCL) do indicador original.
2. **Validação de sinal** – A variação absoluta abertura/fechamento do último candle deve ser maior que o limiar dinâmico, maior que `MinCriteriaPoints * PriceStep` e menor que `MaxCriteriaPoints * PriceStep`. Sinais são ignorados se o candle anterior já atendeu à mesma condição, o que reflete o consultor especialista original.
3. **Direção** – Uma variação negativa (candle de baixa) sugere uma recuperação de alta, portanto uma posição comprada é aberta. Uma variação positiva implica uma expectativa de reversão baixista e aciona uma posição vendida. Novas operações são enviadas apenas quando não há posição existente.
4. **Gestão de risco** – Após a entrada, a estratégia monitora os candles subsequentes. Se o preço toca os níveis predefinidos de stop-loss ou take-profit (convertidos de pontos para preços absolutos usando o `PriceStep` do instrumento), ela fecha imediatamente a posição aberta usando ordens de mercado. `StartProtection()` também é habilitado para suportar as proteções integradas do StockSharp.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `StopLossPoints` | Distância de stop-loss em pontos (multiplicado por `PriceStep`). |
| `TakeProfitPoints` | Distância de take-profit em pontos. |
| `TradeVolume` | Volume usado para cada ordem de mercado. |
| `SlippagePoints` | Configuração informativa que reflete a versão MQL; atualmente não aplicada às ordens. |
| `MinCriteriaPoints` | Distância mínima abertura/fechamento (em pontos) necessária para um sinal válido. |
| `MaxCriteriaPoints` | Distância máxima abertura/fechamento permitida (em pontos). |
| `Coefficient` | Multiplicador usado para construir o limiar de confiança dinâmico. |
| `MaPeriod` | Comprimento da média móvel usada dentro do indicador. Deve ser pelo menos 3. |
| `CandleType` | Período dos candles processados (padrão: 1 hora). |

## Diretrizes de uso
- Certifique-se de que o instrumento selecionado tem um `PriceStep` válido. Quando não disponível, a estratégia recorre a um passo de 1.0, o que pode distorcer os limiares.
- Ajuste `MinCriteriaPoints` e `MaxCriteriaPoints` para combinar com a volatilidade do período escolhido. Uma janela muito estreita filtrará a maioria dos sinais, enquanto uma janela muito ampla permitirá movimentos extremamente grandes que podem não reverter.
- O `Coefficient` padrão de 1.618 replica o escalonamento pela proporção áurea do indicador original. Valores mais altos exigem candles de maior amplitude antes de operar.
- Como as posições são fechadas por ordens de mercado no próximo fechamento de candle que viola os níveis de stop ou alvo, a execução real pode diferir dos níveis limite exatos. Considere testar com dados intradiários para controle mais preciso se necessário.
- Apenas uma posição é mantida de cada vez. A estratégia aguardará a operação atual fechar antes de reagir a um novo sinal.

## Notas
- Realize backtesting da configuração em dados históricos antes de usá-la ao vivo. O EA original foi projetado para mercados FX; ajuste de parâmetros pode ser necessário para outros ativos.
- O parâmetro `SlippagePoints` é preservado para completude mas intencionalmente não usado porque o StockSharp lida com slippage de forma diferente do MetaTrader.
- Certifique-se de que `MaPeriod` permaneça em 3 ou acima; valores menores eram proibidos na implementação original e podem levar a limiares instáveis.
