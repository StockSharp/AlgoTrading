# Estratégia Adaptativa do Cyberia Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Cyberia Trader Adaptive Strategy** é uma versão C# do clássico consultor especialista MetaTrader "CyberiaTrader". O
A estratégia reconstrói o núcleo original baseado em probabilidade em StockSharp e o aumenta com filtros técnicos opcionais.
Ele analisa continuamente as oscilações de preços para medir as chances de reversões e, opcionalmente, confirma o sinal com EMA,
MACD, CCI, ADX ou filtros fractais antes de enviar pedidos.

## Motor de probabilidade
O coração da estratégia é a calculadora de probabilidade inspirada na versão MQL. Ele usa um período de amostragem adaptativo
(`ValuePeriod`) e inspeciona barras históricas em etapas fixas para classificar cada barra como:

* **Probabilidade de venda** – barra de alta seguindo uma barra de baixa (potencial oportunidade de desvanecimento).
* **Probabilidade de compra** – barra de baixa após uma barra de alta.
* **Probabilidade indefinida** – todas as outras configurações de barra.

Para cada classe, a estratégia acumula estatísticas médias de amplitude, taxa de acerto e taxa de sucesso em `ValuePeriod × HistoryMultiplier`
amostras. A pesquisa adaptativa verifica os períodos de `1` a `MaxPeriod` (padrão 23) e mantém o período que produz o maior
taxa de sucesso. Essas estatísticas são expostas internamente como:

* `BuyPossibility`, `SellPossibility`, `UndefinedPossibility` – valores atuais de classificação de barras.
* `BuyPossibilityMid`, `SellPossibilityMid`, ... – médias executadas usadas pela árvore de decisão original.
* `PossibilityQuality`, `PossibilitySuccessQuality` – índices de qualidade usados para diagnóstico e seleção automática de período.

Quando o histórico disponível é insuficiente, a estratégia simplesmente espera até que o mecanismo de probabilidade relate um conjunto de amostras válido.

## Filtros indicadores
O EA original permitia ativar ou desativar módulos adicionais baseados em indicadores. O porto mantém a mesma ideia:

* **EMA filtro** – compara a inclinação de um EMA (`MaPeriod`) entre as duas últimas velas concluídas.
* **MACD filtro** – verifica a relação entre MACD e sua linha de sinal (`MacdFast`, `MacdSlow`, `MacdSignal`).
* **CCI filtro** – sinaliza regimes de sobrecompra/sobrevenda usando limites de `CciPeriod` e ±100.
* **ADX filtro** – inspeciona os componentes +DI e −DI (`AdxPeriod`) para preferir a direção dominante.
* **Filtro Fractal** – detecta a oscilação mais recente usando uma janela `FractalDepth` configurável e bloqueia pedidos nela.
* **Detector de reversão** – alterna os sinalizadores de direção quando um pico de probabilidade excede `ReversalIndex` vezes sua média.

Cada módulo pode ser alternado por meio de parâmetros e reflete o comportamento das entradas externas booleanas originais.

## Lógica de negociação
1. Assine a série de velas configurada (`CandleType`).
2. Reconstrua as estatísticas de probabilidade e, opcionalmente, selecione novamente o período de amostragem ideal em cada vela finalizada.
3. Aplique os filtros de indicadores opcionais e a árvore de decisão da Cyberia para ativar ou desativar as instruções de compra/venda.
4. Execute negociações quando uma decisão de compra ou venda estiver ativa, respeitando as opções globais `BlockBuy` e `BlockSell`.
5. Opcionalmente, aplique proteção absoluta de stop-loss ou take-profit se `StopLossPoints` ou `TakeProfitPoints` forem especificados.
6. Feche as posições antecipadamente quando a decisão se tornar `Unknown` e a qualidade da probabilidade se deteriorar.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas usadas para cálculos. |
| `AutoSelectPeriod` | Ativa a pesquisa adaptativa em `MaxPeriod` para encontrar a melhor janela de amostragem. |
| `InitialPeriod` | Período de probabilidade de fallback quando a seleção automática está desativada. |
| `MaxPeriod` | Período máximo considerado durante a busca adaptativa (padrão 23 como EA). |
| `HistoryMultiplier` | Número de amostras por período utilizado nas estatísticas (padrão 5). |
| `SpreadFilter` | Movimento mínimo (em unidades de preço) necessário para tratar uma probabilidade como “bem-sucedida”. |
| `EnableCyberiaLogic` | Alterna a árvore de decisão original que compara médias de probabilidade. |
| `EnableMa`, `EnableMacd`, `EnableCci`, `EnableAdx`, `EnableFractals`, `EnableReversalDetector` | Habilite filtros individuais. |
| `MaPeriod` | EMA comprimento para o filtro de média móvel. |
| `MacdFast`, `MacdSlow`, `MacdSignal` | Configuração MACD. |
| `CciPeriod` | Comprimento do índice do canal de commodities. |
| `AdxPeriod` | Comprimento médio do índice direcional. |
| `FractalDepth` | Número ímpar de velas analisadas para detectar a oscilação fractal mais recente. |
| `ReversalIndex` | Multiplicador usado pelo detector de reversão. |
| `BlockBuy`, `BlockSell` | Hard switches que param de abrir negociações em uma determinada direção. |
| `TakeProfitPoints`, `StopLossPoints` | Distâncias opcionais de take-profit e stop-loss absolutos. |

## Notas
* A pesquisa de período adaptativo requer histórico suficiente: `ValuePeriod × HistoryMultiplier + ValuePeriod` barras.
* Todos os comentários foram reescritos em inglês e a lógica se mantém em alto nível StockSharp API com ligações de indicadores.
* As métricas de probabilidade são campos internos, mas expostos por meio de logs ou pela extensão da estratégia se forem necessários diagnósticos adicionais.
