# A estratégia do perfurador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Puncher é um sistema de reversão de momento convertido do consultor especialista MetaTrader 4 original "The Puncher by L. Bigger". Ele combina um oscilador Stochastic lento com um filtro RSI clássico para negociar condições extremas de sobrecompra e sobrevenda. Quando ambos os osciladores concordam que o mercado está estendido, a estratégia procura uma reversão no fechamento da vela e insere uma ordem de mercado na direção oposta.

## Lógica de negociação
- **Configuração de compra:** Acionada quando a linha de sinal Stochastic e RSI caem simultaneamente abaixo do nível de sobrevenda. A posição curta existente, se houver, é fechada primeiro e, em seguida, uma nova posição longa é aberta.
- **Configuração de venda:** Acionada quando ambos os osciladores sobem acima do nível de sobrecompra. Qualquer posição longa aberta é liquidada antes que uma nova posição curta seja colocada.
- **Regras de saída:** As posições são fechadas por sinais opostos ou por regras de proteção (stop-loss, take-profit, ponto de equilíbrio e trailing stop).

A estratégia processa apenas velas finalizadas do período selecionado para evitar ruído intra-barra e replica o comportamento de "negociação no fechamento da barra" da fonte EA.

## Gestão de risco
- **Stop-loss/take-profit:** Distâncias fixas opcionais medidas em pips. Quando desabilitado (zero), a proteção correspondente é ignorada.
- **Ponto de equilíbrio:** Move o stop para o preço de entrada após a negociação acumular o buffer de lucro solicitado.
- **Trailing stop:** Segue o preço com uma distância configurável e passo mínimo para que o stop seja reduzido somente depois que o preço avançar o suficiente.
- **Volume:** Os pedidos usam um parâmetro de volume fixo, refletindo a entrada de tamanho de lote da versão MT4.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume de negociação para novas entradas. | `1` |
| `StochasticLength` | Comprimento de lookback do oscilador Stochastic (%K). | `100` |
| `StochasticSignalPeriod` | Período de suavização de %K antes de aplicar a linha de sinal. | `3` |
| `StochasticSmoothingPeriod` | Período de suavização para a linha de sinal %D. | `3` |
| `RsiPeriod` | Período de cálculo do filtro RSI. | `14` |
| `OversoldLevel` | Limite compartilhado pelos osciladores para detectar condições de sobrevenda. | `30` |
| `OverboughtLevel` | Limite compartilhado pelos osciladores para detectar condições de sobrecompra. | `70` |
| `StopLossPips` | Distância da parada de proteção (0 desabilita). | `2000` |
| `TakeProfitPips` | Distância da meta de lucro (0 desativa). | `0` |
| `TrailingStopPips` | Distância de parada final (0 desativa). | `0` |
| `TrailingStepPips` | Movimento mínimo favorável antes de apertar o trailing stop. | `1` |
| `BreakEvenPips` | Lucro necessário antes de mover o stop para o ponto de equilíbrio. | `0` |
| `CandleType` | Tipo de dados usado para construir velas. | `M15` |

## Notas
- O tamanho do pip é derivado da etapa de preço do título ou dos decimais, garantindo que as distâncias de stop e trailing respeitem a precisão do instrumento.
- A estratégia é adequada para backtests discricionários onde o EA original foi usado e pode servir como base para melhorias adicionais em StockSharp.
- Alertas de áudio, e-mails e rótulos nos gráficos da versão MT4 são omitidos intencionalmente porque são recursos específicos da plataforma.
