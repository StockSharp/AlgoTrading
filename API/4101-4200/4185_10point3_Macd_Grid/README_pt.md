# Estratégia de grade TenPointThree MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma porta C# do consultor especialista MetaTrader **10p3v003 (10point3.mq4)**. Ele combina um gatilho cruzado MACD com um mecanismo de grade martingale. A lógica original foi replicada usando StockSharp de alto nível API com os seguintes comportamentos principais:

- **MACD lógica de sinal** – Uma direção de negociação é determinada quando a linha principal MACD cruza a linha de sinal na barra deslocada (`SignalShift`). As entradas longas exigem que o valor do sinal anterior esteja abaixo de `-TradingRangePips`, o valor atual de MACD fique abaixo de zero e vice-versa para as posições curtas. Os sinais podem opcionalmente ser invertidos através de `ReverseSignal`.
- **Camadas de grade** – Após a abertura da primeira posição, entradas adicionais na mesma direção só serão permitidas quando o preço se mover em relação ao último preenchimento em pelo menos `GridStepPips`. Cada nova perna multiplica o volume por `LotMultiplier` (ou por `1.5` se `MaxTrades > 12`), imitando a escala martingale de MQL4.
- **Proteção contra riscos** – A etapa mais recente é fechada e nenhuma entrada adicional é adicionada quando `OrdersToProtect` ou mais negociações estão ativas e o lucro flutuante excede o limite monetário. O limite é baseado na porcentagem de risco configurada (gestão de dinheiro habilitada) ou na heurística de tamanho do contrato (gestão de dinheiro desabilitada).
- **Saídas por perna** – Cada perna rastreia seu próprio take-profit, stop-loss virtual e trailing stop. A distância de parada corresponde à fórmula original: `InitialStopPips + (MaxTrades - existingOrders) * GridStepPips`. O trailing é ativado somente após o preço se mover em `TrailingStopPips + GridStepPips` a favor da posição e fecha a perna quando o preço retrocede em `TrailingStopPips`.
- **Filtro de sessão** – Quando `UseTimeFilter` está ativado, nenhuma nova grade é iniciada enquanto o tempo da vela estiver estritamente entre `StopHour` e `StartHour`, reproduzindo a proteção de "fuso horário de perigo" do script.

Todas as conversões de dinheiro usam os metadados `PriceStep`/`StepPrice` do título. Se a exchange não expor o tamanho do contrato, um valor substituto de `100000` será aplicado, o que corresponde à suposição original do Forex.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `CandleType` | Assinatura Candle usada para processamento de MACD (padrão: período de 30 minutos). |
| `Volume` | Tamanho base do lote para a primeira ordem de grade. |
| `TakeProfitPips` | Distância em pips para o take-profit de cada perna (0 desabilita). |
| `InitialStopPips` | Distância de parada base em pips. A parada real aumenta com o número de slots livres na grade. |
| `TrailingStopPips` | Distância de trailing stop em pips aplicada após a perna ser suficientemente lucrativa (0 desabilita). |
| `MaxTrades` | Número máximo de entradas simultâneas de martingale. |
| `LotMultiplier` | Multiplicador aplicado ao volume de cada perna de grade adicional (substituído por `1.5` quando `MaxTrades > 12`). |
| `GridStepPips` | Movimento adverso mínimo de preço (em pips) necessário antes de abrir a próxima entrada na grade. |
| `OrdersToProtect` | Número mínimo de pernas ativas antes que a proteção de lucro flutuante possa fechar a última negociação. |
| `UseMoneyManagement` | Permite o cálculo dinâmico de lotes com base no patrimônio da conta. |
| `AccountType` | Seleciona a fórmula de risco: `0` – Padrão (patrimônio líquido / 10.000); `1` – Normal (patrimônio líquido/100.000); `2` – Nano (patrimônio líquido/1.000). |
| `RiskPercent` | Percentagem de capital próprio utilizado quando a gestão de dinheiro está ativada. |
| `ReverseSignal` | Inverte sinais MACD longos/curtos. |
| `FastEmaLength`, `SlowEmaLength`, `SignalLength` | MACD períodos (26/12/9 por padrão). |
| `SignalShift` | Número de barras fechadas usadas para a verificação de cruzamento (padrão: 1). |
| `TradingRangePips` | MACD banda de sinal (em pips) que deve ser violada antes que um cruzamento seja aceito. |
| `UseTimeFilter` | Ativa a proteção de sessão com base em `StopHour`/`StartHour`. |
| `StopHour`, `StartHour` | Faixa exclusiva que bloqueia a criação de uma nova grade quando `UseTimeFilter` for verdadeiro. |

## Notas de gerenciamento de dinheiro

Quando `UseMoneyManagement` está desativado, o lote base (`Volume`) é usado diretamente. Caso contrário, o EA calcula o tamanho do lote a partir do patrimônio atual usando as mesmas fórmulas do EA original:

- Tipo de conta **0**: `Ceil(risk% * equity / 10,000) / 10`
- Tipo de conta **1**: `risk% * equity / 100,000`
- Tipo de conta **2**: `risk% * equity / 1,000`

Os volumes são normalizados com `Security.VolumeStep` e, em seguida, limitados por `Security.MinVolume`/`MaxVolume`.

## Fluxo de trabalho de execução

1. Assine o fluxo de velas configurado e alimente o indicador MACD por meio de `BindEx`.
2. Em cada vela finalizada, atualize a lógica de trailing/stop para as pernas ativas.
3. Quando as regras de cruzamento MACD forem acionadas, certifique-se de que o filtro da sessão permita a negociação, que a direção da grade corresponda à posição existente e que o preço tenha se movido `GridStepPips` em relação ao último preenchimento.
4. Calcule o volume da próxima perna usando o multiplicador de martingale e envie uma ordem de mercado.
5. Monitore o lucro flutuante; assim que o limite de proteção for atingido, feche a perna mais recente e faça uma pausa até a próxima vela.

## Notas de conversão

- Todos os comentários foram reescritos em inglês conforme necessário.
- É usado StockSharp API de alto nível (velas + `BindEx`). O acesso direto ao valor do indicador é evitado.
- Os cálculos de lucro flutuante baseiam-se em `PriceStep`/`StepPrice`. Para instrumentos exóticos certifique-se de que estes campos estejam preenchidos.
- A estratégia mantém o estado por trecho internamente para emular o gerenciamento de pedidos MQL4, porque StockSharp agrega posições por padrão.
