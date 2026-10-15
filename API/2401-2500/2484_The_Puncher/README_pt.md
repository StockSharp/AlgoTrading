# Estratégia The Puncher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertido do consultor especialista do MetaTrader 5 "The Puncher".
- Usa um oscilador Estocástico de longo período combinado com RSI para identificar zonas de exaustão.
- Opera apenas quando a vela atual está fechada, seguindo a abordagem da API de alto nível do StockSharp.
- Aplica lógica de stop-loss protetor, take-profit, ponto de equilíbrio e stop móvel para gerenciar o risco.

## Indicadores
- **Oscilador Estocástico**: período base `StochasticPeriod`, suavização %K `StochasticSignalPeriod`, suavização %D `StochasticSmoothingPeriod`.
- **Índice de Força Relativa (RSI)**: período `RsiPeriod`.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `StochasticPeriod` | 100 | Período base do oscilador Estocástico. |
| `StochasticSignalPeriod` | 3 | Período de suavização aplicado à linha %K. |
| `StochasticSmoothingPeriod` | 3 | Período de suavização aplicado à linha %D. |
| `RsiPeriod` | 14 | Comprimento de cálculo do RSI. |
| `OversoldLevel` | 30 | Limiar compartilhado pelo Estocástico e RSI para detectar zonas de sobrevenda. |
| `OverboughtLevel` | 70 | Limiar compartilhado pelo Estocástico e RSI para detectar zonas de sobrecompra. |
| `StopLossPips` | 20 | Distância do stop-loss em pips (0 desativa o stop-loss). |
| `TakeProfitPips` | 50 | Distância do take-profit em pips (0 desativa o take-profit). |
| `TrailingStopPips` | 10 | Distância do stop móvel em pips (0 desativa o trailing). |
| `TrailingStepPips` | 5 | Movimento favorável mínimo em pips necessário antes de ajustar o stop móvel novamente. |
| `BreakEvenPips` | 21 | Lucro em pips necessário antes de mover o stop para o ponto de equilíbrio (0 desativa). |
| `CandleType` | Período de 5 minutos | Tipo de vela utilizado para os cálculos. |
| `Volume` | Propriedade da estratégia | Tamanho da ordem utilizado para entradas (configurado via `Volume` da estratégia). |

> **Tratamento de pips**: os parâmetros baseados em pips são convertidos para preços absolutos usando `Security.PriceStep`. Ajuste `Security.PriceStep` para o instrumento que você opera.

## Regras de trading
### Entrada
- **Comprado**: quando a linha de sinal do Estocástico e o RSI caem abaixo de `OversoldLevel`, e não há posição comprada existente.
- **Vendido**: quando a linha de sinal do Estocástico e o RSI sobem acima de `OverboughtLevel`, e não há posição vendida existente.
- Se um sinal contrário aparecer enquanto há uma posição aberta, a estratégia fecha a posição e aguarda a próxima vela antes de considerar novas entradas.

### Saída e gestão de risco
- **Stop-loss**: distância fixa definida por `StopLossPips`.
- **Take-profit**: alvo fixo definido por `TakeProfitPips`.
- **Ponto de equilíbrio**: uma vez que o lucro atinge `BreakEvenPips`, o stop é movido para o preço de entrada.
- **Stop móvel**: após o preço se mover favoravelmente em `TrailingStopPips`, o stop acompanha o mercado e é ajustado a cada `TrailingStepPips`.
- **Sinais contrários**: forçam uma saída mesmo que o stop ou o alvo não tenham sido atingidos.

## Notas
- Funciona com qualquer instrumento suportado pelo StockSharp; os valores padrão são ajustados para pips de câmbio.
- Usa apenas velas concluídas, reproduzindo o comportamento `TradeAtCloseBar=true` do robô original.
- Configure o portfólio, o instrumento e o volume antes de iniciar a estratégia.
