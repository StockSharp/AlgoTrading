# Estratégia Auto RXD v1.67
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Auto RXD v1.67 é uma estratégia baseada em regras que emula o consultor especialista MetaTrader de mesmo nome. A abordagem utiliza três perceptrons lineares: um supervisor que decide se procura sinais de alta ou de baixa, além de um perceptron dedicado para cada direção. Cada perceptron opera com médias móveis ponderadas lineares (LWMAs) calculadas a partir do fechamento da vela e das entradas de "preço ponderado" de Robbie Ruan (máximo + mínimo + 2 × fechamento). A porta StockSharp é executada apenas em velas concluídas e usa o fluxo de dados de alto nível `BindEx` para manter os cálculos do indicador sincronizados com o ciclo de negociação.

## Dados e Indicadores de Mercado
- **Velas** – O prazo padrão é velas de 30 minutos. O prazo pode ser alterado através do parâmetro `CandleType`.
- **Average True Range (ATR)** – Fornece distâncias adaptáveis de take-profit e stop loss quando `UseAtrTargets` está ativado. O período ATR é controlado por `AtrPeriod`.
- **Índice de força relativa (RSI)** – Filtro opcional que impõe negociações longas acima do nível neutro 50 e vendas abaixo de 50 quando `UseRsiFilter` for verdadeiro.
- **Índice de canal de commodities (CCI)** – Filtro de tendência opcional que requer leituras acima de +100 para posições compradas e abaixo de -100 para posições vendidas quando `UseCciFilter` está ativo.
- **Moving Average Convergence Divergence (MACD)** – Confirmação de impulso opcional. As entradas longas requerem a linha MACD acima da linha de sinal, enquanto as entradas curtas precisam da linha MACD abaixo da linha de sinal quando `UseMacdFilter` é verdadeiro.
- **Índice Direcional Médio (ADX)** – Filtro de força opcional que verifica se ADX está acima do limite configurado e se +DI versus -DI se alinha com a direção desejada quando `UseAdxFilter` está ativado.

## Lógica de negociação
1. **Preparação de dados Perceptron** – Para cada vela, a estratégia atualiza os buffers com os últimos preços de fechamento e ponderados. Os buffers alimentam instantâneos LWMA, gerando quatro recursos defasados ​​separados pelos valores `Step` configurados para perceptrons curtos, longos e supervisores.
2. **Decisão do Supervisor** – O perceptron do supervisor avalia os deltas defasados usando os parâmetros de peso `SupervisorX1…X4` e `SupervisorThreshold`. Uma pontuação positiva desbloqueia o longo perceptron; uma pontuação negativa desbloqueia o perceptron curto. Se a pontuação do supervisor for zero ou indisponível (dados insuficientes), a vela será ignorada.
3. **Especialistas direcionais** – O perceptron correspondente (longo ou curto) valida sua própria pontuação usando o mesmo conjunto de recursos LWMA e pesos específicos de direção (`LongX*` ou `ShortX*`). Um valor positivo aciona o próximo estágio de validação.
4. **Filtros Indicadores** – Quando `UseIndicatorFilters` é falso, a estratégia é negociada apenas no sinal perceptron. Quando verdadeiro, cada filtro ativado (RSI, CCI, MACD, ADX) deve concordar com a direção proposta. Dados do indicador ausentes ou condições de falha cancelam o sinal.
5. **Execução de ordens** – A estratégia garante que não haja ordens ativas, nivela qualquer exposição oposta e entra usando ordens de mercado dimensionadas em `OrderVolume`. Os preços de entrada são padronizados para a melhor cotação quando disponível, caso contrário a vela fecha.

## Gestão de risco
- **Ordens de proteção** – Depois de preencher uma entrada, a estratégia calcula imediatamente as distâncias de take-profit e stop loss através de `CalculateProtectiveDistances`. Quando `UseAtrTargets` é verdadeiro, as distâncias são escalonadas ATR pelos multiplicadores configurados (`AtrTakeProfitFactor`, `AtrStopLossFactor`) e pelas magnitudes TP/SL baseadas em pontos originais MQL. Se a segmentação ATR estiver desativada, as distâncias de pontos fixos serão convertidas em etapas de preço.
- **Gerenciamento de pedidos** – O auxiliar `SetProtectiveOrders` traduz distâncias brutas em contagens de etapas de preço e registra ordens de stop-loss e take-profit em relação ao preço de entrada. A estratégia evita pedidos duplicados verificando `HasActiveOrders()` antes de enviar novas negociações.
- **Iniciar proteção** – `StartProtection()` é chamado uma vez em `OnStarted`, permitindo o tratamento de proteção integrado da estrutura sempre que a posição se torna diferente de zero.

## Parâmetros
A implementação StockSharp expõe o conjunto completo de parâmetros MQL agrupados para otimização e clareza da IU. Os principais parâmetros incluem:

### Negociação
- `OrderVolume` – Tamanho do lote para novas posições.
- `CandleType` – Tipo de dados Candle usado para vinculação.

### Risco
- `UseAtrTargets` – Alternar entre distâncias de proteção baseadas em ATR e de ponto fixo.
- `AtrPeriod`, `AtrTakeProfitFactor`, `AtrStopLossFactor` – ATR configuração para destinos adaptativos.
- `LongTakeProfitPoints`, `LongStopLossPoints`, `ShortTakeProfitPoints`, `ShortStopLossPoints` – Referências TP/SL baseadas em pontos reutilizadas por ATR e modos fixos.

### Filtros Indicadores
- `UseIndicatorFilters` – Chave mestre para todos os filtros.
- `UseAdxFilter`, `AdxPeriod`, `AdxThreshold` – ADX configurações de confirmação.
- `UseMacdFilter`, `MacdFast`, `MacdSlow`, `MacdSignal` – MACD configurações de confirmação.
- `UseRsiFilter`, `RsiPeriod` – RSI configurações de confirmação.
- `UseCciFilter`, `CciPeriod` – CCI configurações de confirmação.

### Especialistas em Perceptron
- `ShortMaPeriod`, `ShortStep`, `ShortX1…ShortX4`, `ShortThreshold` – Configuração curta de perceptron.
- `LongMaPeriod`, `LongStep`, `LongX1…LongX4`, `LongThreshold` – Configuração de perceptron longo.
- `SupervisorMaPeriod`, `SupervisorStep`, `SupervisorX1…SupervisorX4`, `SupervisorThreshold` – Configuração do perceptron do supervisor.

Todos os parâmetros numéricos espelham os padrões MQL, permitindo um comportamento semelhante entre o consultor especialista original e esta porta StockSharp enquanto expõe a configuração por meio do sistema `StrategyParam` para campanhas de otimização.
