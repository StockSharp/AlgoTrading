# Estratégia MACD Especialista Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o robô original "MACD Expert" do MetaTrader dentro do framework StockSharp. Sincroniza tendências do MACD em quatro períodos de tempo—5 minutos, 15 minutos, 1 hora e 4 horas—e só permite uma nova posição quando todos os períodos apontam na mesma direção. O objetivo é capturar o alinhamento de momentum multi-período enquanto filtra períodos de spread alto.

## Dados e indicadores
- **Velas**: 5m (execução), 15m, 1h e 4h confirmações. Todas as velas usam preços de fechamento e apenas barras terminadas.
- **Indicador**: `MovingAverageConvergenceDivergenceSignal` com padrões 12/26/9. Cada período tem sua própria instância de MACD para que os sinais não interfiram.
- **Cotizações de Nível 1**: As melhores cotizações de oferta/demanda são consumidas para monitorar o spread ao vivo antes de abrir operações.

## Lógica de negociação
1. Aguardar que as quatro instâncias de MACD emitam um valor completado.
2. Calcular a relação entre a linha MACD e a linha de sinal em cada período.
3. Aplicar um filtro de spread máximo medido em pontos de preço (passos de preço).
4. Abrir no máximo uma posição de cada vez; as posições existentes devem terminar via stop-loss ou take-profit antes que uma nova ordem seja permitida.

### Configuração comprada
- A linha de sinal MACD está acima da linha MACD em **todos** os períodos monitorados.
- O spread não excede `MaxSpreadPoints`.
- Uma posição comprada é aberta com `OrderVolume` lotes no fechamento da última vela de 5 minutos.

### Configuração vendida
- A linha de sinal MACD está abaixo da linha MACD em **todos** os períodos monitorados.
- O spread não excede `MaxSpreadPoints`.
- Uma posição vendida é aberta com `OrderVolume` lotes no fechamento da última vela de 5 minutos.

### Gestão de posição
- Operações compradas colocam alvos lógicos a `TakeProfitPoints` acima da entrada e stops a `StopLossPoints` abaixo.
- Operações vendidas colocam alvos lógicos a `TakeProfitPoints` abaixo da entrada e stops a `StopLossPoints` acima.
- As saídas são acionadas quando a máxima/mínima intrabarra de uma vela de 5 minutos terminada toca o alvo ou nível de stop respectivo.
- Enquanto em posição, a estratégia ignora os sinais opostos; espera até que a operação seja fechada por stop ou take-profit antes de reagir novamente, correspondendo à lógica MQL original.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Tamanho da posição em lotes (espelha o input `Lots` da versão MQL). |
| `StopLossPoints` | 200 | Distância ao stop de proteção em pontos de preço. |
| `TakeProfitPoints` | 400 | Distância ao alvo de lucro em pontos de preço. |
| `MaxSpreadPoints` | 20 | Spread máximo permitido em pontos de preço antes de as entradas serem ignoradas. |
| `FastPeriod` | 12 | Comprimento da EMA rápida dentro de cada instância de MACD. |
| `SlowPeriod` | 26 | Comprimento da EMA lenta dentro de cada instância de MACD. |
| `SignalPeriod` | 9 | Comprimento da EMA de sinal dentro de cada instância de MACD. |
| `FiveMinuteCandleType` | Velas de 5 minutos | Período de execução principal. |
| `FifteenMinuteCandleType` | Velas de 15 minutos | Primeiro período de confirmação. |
| `HourCandleType` | Velas de 1 hora | Segundo período de confirmação. |
| `FourHourCandleType` | Velas de 4 horas | Terceiro período de confirmação. |

## Notas de implementação
- Usa `BindEx` para ler valores de MACD fortemente tipados sem chamar `GetValue`, seguindo as diretrizes do projeto.
- Um auxiliar compartilhado converte a relação MACD/sinal em sinalizadores `{-1, 0, 1}` para simplificar as verificações de confirmação.
- A validação do spread divide a melhor oferta de venda menos a melhor oferta de compra por `Security.PriceStep` para que o limiar corresponda ao comportamento de "pontos" do MetaTrader.
- Eventos de operação são registrados com `LogInfo` para auxiliar na depuração ao testar no Designer ou Runner.
- Nenhuma tradução de Python é fornecida, conforme os requisitos da tarefa; apenas a versão C# está incluída.
