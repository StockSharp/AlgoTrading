# Melhor estratégia do TradingLab MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista MetaTrader "TradingLab_Best_MACD_Strategy" usando o StockSharp de alto nível de API. Ele combina estrutura de média móvel, cruzamentos MACD e verificações dinâmicas de suporte/resistência para abrir negociações direcionais que se alinham com o impulso e as reações recentes dos preços.

## Lógica principal

- **Fonte da vela** – Usa o parâmetro configurável `CandleType` para assinar velas finalizadas. Somente velas concluídas geram decisões de negociação.
- **Filtro de tendência** – Uma média móvel simples de 200 períodos define a tendência predominante. As negociações longas exigem que o fechamento permaneça acima da média, enquanto as negociações curtas exigem que o fechamento permaneça abaixo dela.
- **Caixa de suporte e resistência** – Uma janela de 20 períodos mais alto/mais baixo emula o indicador personalizado "Caixa". Tocar no nível de resistência ou suporte anterior arma configurações curtas ou longas para um número limitado de velas controladas por `SignalValidity`.
- **MACD Crossovers** – Um MACD padrão (12, 26, 9 por padrão) deve cruzar sua linha de sinal na vela anterior e permanecer no lado requerido da linha zero. Cada cruzamento válido mantém seu sinal ativo por `SignalValidity` velas, espelhando a lógica de contagem regressiva da fonte EA.
- **Tempo de entrada** – Uma posição é aberta quando o MACD e o toque de suporte/resistência correspondente ainda são válidos e pelo menos um deles é acionado na vela atual.
- **Lógica de saída** – Na entrada, as metas dinâmicas de stop-loss e take-profit são calculadas em relação à distância média móvel. A distância de take-profit é `RiskRewardMultiplier` vezes a distância ajustada usada para o stop. As saídas de proteção monitoram as velas subsequentes e chamam `ClosePosition()` quando o preço ultrapassa os níveis armazenados.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Volume fixo enviado com cada ordem de mercado. |
| `SignalValidity` | Número de velas que mantêm MACD e gatilhos de suporte/resistência ativos. |
| `MaLength` | Período do filtro de tendência de média móvel simples. |
| `BoxPeriod` | Comprimento de lookback para a caixa mais alta/mais baixa que rastreia resistência e suporte recentes. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD períodos rápidos, lentos e de sinal. |
| `StopDistancePoints` | Distância da média móvel ao stop loss, expressa em pontos estilo MetaTrader (multiplicados pela etapa de preço do símbolo). |
| `RiskRewardMultiplier` | Multiplicador aplicado à distância MA ajustada para produzir a meta de lucro. |
| `CandleType` | Tipo de dados que descreve a série de velas a ser assinada (padrão: período de 1 hora). |

## Notas

- A detecção de suporte e resistência segue a ideia original, observando se a vela anterior rompe os níveis mais altos/mais baixos de 20 períodos. Cada toque reinicia os contadores de validade.
- Stops e alvos são recalculados para cada nova entrada e verificados em relação ao máximo/mínimo de cada vela finalizada para imitar o monitoramento intrabarra de MetaTrader de forma determinística.
- O gerenciamento de proteção depende do instrumento `PriceStep`. Se um instrumento reportar um passo zero, um fallback de 0,0001 será usado.
