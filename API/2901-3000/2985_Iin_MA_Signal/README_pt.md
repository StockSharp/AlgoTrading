# Estratégia Iin MA Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o comportamento do clássico consultor especialista MQL5 **Iin MA Signal**. Ela observa um cruzamento entre uma média móvel rápida e uma lenta e reage na barra definida pelo parâmetro `SignalBar`, assim como o template original que consultava os buffers do indicador. Cruzamentos altistas abrem posições compradas e opcionalmente fecham vendidos existentes, enquanto cruzamentos baixistas abrem vendidos e opcionalmente fecham comprados. Stops e alvos podem ser anexados automaticamente através da proteção de posição do StockSharp.

## Lógica de negociação
1. Assinar uma única série de candles especificada por `CandleType` (padrão: candles de 1 hora).
2. Construir duas médias móveis usando os tipos e comprimentos definidos por `FastMaType`/`FastPeriod` e `SlowMaType`/`SlowPeriod`. SMA, EMA, SMMA (RMA) e LWMA são suportados para cobrir as combinações disponíveis no código-fonte MQL.
3. Armazenar uma janela deslizante de valores de média móvel para que o cruzamento possa ser avaliado no índice de candle dado por `SignalBar`. Isso imita as solicitações `CopyBuffer` do consultor especialista original.
4. Detectar um cruzamento altista quando a MA rápida estava abaixo da MA lenta na barra anterior da janela e sobe acima dela na barra de sinal enquanto a tendência anterior não era já altista. Um cruzamento baixista é detectado de forma simétrica.
5. Atualizar o sinalizador de tendência interno após cada cruzamento confirmado para evitar entradas duplicadas e replicar a variável guarda `trend` do indicador MQL.
6. Quando a negociação é permitida (`IsFormedAndOnlineAndAllowTrading()` retorna verdadeiro), enviar as ordens de mercado definidas pelos sinalizadores de entrada/saída.

## Regras de entrada
- **Entrada comprada**: ativada em um cruzamento altista se `AllowLongEntries` estiver habilitado e a posição atual for plana ou vendida. Qualquer vendido aberto pode ser fechado primeiro quando `CloseShortOnSignal` é verdadeiro.
- **Entrada vendida**: ativada em um cruzamento baixista se `AllowShortEntries` estiver habilitado e a posição atual for plana ou comprada. Qualquer comprado aberto pode ser fechado primeiro quando `CloseLongOnSignal` é verdadeiro.

## Regras de saída
- Sinais opostos podem fechar posições de acordo com os interruptores `CloseLongOnSignal` e `CloseShortOnSignal`.
- Níveis opcionais de saída protetora usam distâncias de preço absolutas: `StopLossPoints` e `TakeProfitPoints`. Quando algum dos valores é maior que zero, a estratégia chama `StartProtection` para armar o stop-loss e/ou take-profit usando ordens de mercado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de dados que descreve a série de candles usada para cálculos. | Período de 1 hora |
| `FastPeriod` | Período da média móvel rápida. | 10 |
| `FastMaType` | Tipo da média móvel rápida (`Sma`, `Ema`, `Smma`, `Lwma`). | `Ema` |
| `SlowPeriod` | Período da média móvel lenta. | 22 |
| `SlowMaType` | Tipo da média móvel lenta (`Sma`, `Ema`, `Smma`, `Lwma`). | `Sma` |
| `SignalBar` | Número de barras fechadas atrás que devem conter o cruzamento (1 reproduz o padrão MQL). | 1 |
| `AllowLongEntries` | Habilitar ou desabilitar entradas compradas. | `true` |
| `AllowShortEntries` | Habilitar ou desabilitar entradas vendidas. | `true` |
| `CloseLongOnSignal` | Fechar posições compradas quando um sinal baixista aparece. | `true` |
| `CloseShortOnSignal` | Fechar posições vendidas quando um sinal altista aparece. | `true` |
| `StopLossPoints` | Distância absoluta de stop-loss em unidades de preço (0 desabilita). | 1000 |
| `TakeProfitPoints` | Distância absoluta de take-profit em unidades de preço (0 desabilita). | 2000 |

## Notas de implementação
- APIs de alto nível do StockSharp são usadas em todo momento: `SubscribeCandles` solicita dados de mercado e `Bind` transmite os valores de MA diretamente para a estratégia sem gerenciamento manual de histórico.
- A factory de médias móveis (`CreateMa`) mapeia os valores de enum para indicadores StockSharp, evitando cálculos personalizados.
- Um buffer compacto na memória mantém apenas `SignalBar + 2` amostras, o suficiente para avaliar o cruzamento na barra solicitada e na anterior.
- As ordens de proteção são opcionais e são inicializadas apenas se distâncias não nulas forem configuradas, replicando o módulo MM opcional da versão MQL.
- Todos os comentários no código são escritos em inglês de acordo com as regras do repositório.

## Uso
1. Compilar a solução (`dotnet build AlgoTrading.sln`) para compilar a nova estratégia.
2. Instanciar `IinMaSignalStrategy` em sua aplicação, configurar os parâmetros desejados e atribuir um conector/instrumento/portfólio antes de iniciá-lo.
3. Opcionalmente anexar a estratégia a um gráfico para visualizar as médias móveis rápida e lenta junto com as negociações executadas.
4. Otimizar os períodos de MA, a barra de sinal e as configurações de risco para adaptar o template a diferentes mercados.

## Diferenças em relação ao consultor especialista MQL original
- A versão StockSharp usa assinatura de alto nível e vinculação de indicadores em vez de consultas manuais de buffer.
- Auxiliares de gerenciamento de dinheiro de `TradeAlgorithms.mqh` são substituídos por `StartProtection`, que oferece automação equivalente de stop e alvo.
- O gerenciamento de posições é plano por padrão: a estratégia evita hedging não abrindo uma nova posição enquanto o lado oposto ainda está ativo, a menos que o sinalizador de fechamento esteja desabilitado.
- O renderizador de gráfico aproveita os métodos auxiliares do StockSharp e não tenta replicar os buffers de seta originais.
