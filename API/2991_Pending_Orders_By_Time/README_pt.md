# Estratégia de Ordens Pendentes por Tempo 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia reproduz o comportamento do expert original MetaTrader "Pending orders by time 2" agendando ordens de entrada no estilo rompimento em torno de uma hora de abertura configurável. No início da sessão de negociação, o algoritmo coloca tanto um buy stop acima do preço ask atual quanto um sell stop abaixo do preço bid atual. Cada entrada pendente carrega seus próprios níveis de stop-loss e take-profit expressos em passos de preço do instrumento, e uma vez que uma entrada é acionada, a estratégia mantém a posição aberta com lógica de trailing stop e ordens de saída mutuamente exclusivas. O código é projetado para a API de alto nível do StockSharp e usa tabulações para indentação conforme exigido pelas diretrizes do projeto.

## Fluxo da Sessão de Negociação
1. **Reinício diário** – Na primeira vela concluída de um novo dia de negociação, a estratégia limpa os flags internos para que um novo par de ordens pendentes possa ser emitido mais tarde na sessão.
2. **Colocação na hora de abertura** – Quando a hora da vela é igual à hora de abertura configurada e as ordens ainda não foram colocadas para o dia atual, a estratégia calcula os preços de rompimento relativos ao último snapshot do melhor bid/ask (retorna ao fechamento da vela se nenhuma cotação estiver disponível) e envia tanto buy stop quanto sell stop.
3. **Gerenciamento intradiário** – Enquanto a sessão está ativa, a lógica rastreia o stop protetor para qualquer posição aberta, mantém a entrada pendente oposta ativa (permitindo uma potencial reversão), e aguarda que o trailing stop, o take-profit fixo, ou a ordem de rompimento oposta feche a posição.
4. **Limpeza na hora de fechamento** – Assim que a hora da vela corresponde à hora de fechamento configurada, a estratégia cancela quaisquer ordens de entrada pendentes ainda ativas e fecha a posição líquida a mercado, garantindo que nenhuma negociação seja carregada durante a noite.

## Detalhes de Colocação de Ordens
- **Distância, stop-loss, take-profit** – Os parâmetros `DistanceTicks`, `StopLossTicks` e `TakeProfitTicks` são interpretados em passos de preço do instrumento (`Security.PriceStep`). O preço do buy stop é `bestAsk + DistanceTicks * step`, seu stop-loss é colocado `StopLossTicks` abaixo do preço de entrada, e o take-profit é a mesma distância acima. O sell stop espelha essa lógica no lado curto.
- **Tratamento de bid/ask** – A estratégia se inscreve no livro de ordens e registra continuamente o melhor bid e ask. Se o livro de ordens ainda não forneceu uma cotação, o preço de fechamento da vela terminada é usado como fallback seguro.
- **Referências de ordens** – Referências às ordens pendentes enviadas são armazenadas para que o algoritmo possa cancelá-las ou reregistrá-las quando a sessão mudar ou quando a hora de fechamento for acionada.

## Gestão de Posição e Risco
- **Ordens protetoras** – Quando uma entrada pendente é executada (detectada em `OnOwnTradeReceived`), a estratégia registra imediatamente uma ordem de stop protetor e uma ordem de take-profit com o volume de posição original. Posições longas recebem um `SellStop` e `SellLimit`, enquanto posições curtas recebem um `BuyStop` e `BuyLimit`. Apenas um stop e um take-profit permanecem ativos em qualquer momento; emitir novas ordens protetoras cancela automaticamente o par anterior.
- **Trailing stop** – O trailing é controlado por `TrailingStopTicks` (a distância real do stop) e `TrailingStepTicks` (lucro mínimo necessário antes de um ajuste). A lógica de trailing é acionada assim que o lucro não realizado excede `TrailingStop + TrailingStep`. Ela recalcula um preço de stop melhor (nunca afrouxando o stop atual), cancela a ordem de stop protetor anterior e envia uma nova no nível mais apertado.
- **Saída na hora de fechamento** – Quando a hora de fechamento chega, a estratégia cancela ambas as ordens protetoras e envia uma ordem de mercado no tamanho da posição absoluta para que nenhuma exposição permaneça aberta.

## Parâmetros
- `OpeningHour` – Hora (0–23) em que as ordens pendentes são criadas.
- `ClosingHour` – Hora (0–23) em que as ordens pendentes são removidas e as posições são fechadas.
- `DistanceTicks` – Distância de rompimento do bid/ask atual expressa em passos de preço.
- `StopLossTicks` – Distância protetora fixa para o stop inicial.
- `TakeProfitTicks` – Distância fixa para o objetivo de lucro.
- `TrailingStopTicks` – Distância mantida pelo trailing stop uma vez ativado.
- `TrailingStepTicks` – Lucro adicional mínimo necessário antes que o trailing stop seja movido novamente.
- `Volume` – Tamanho de ambas as ordens pendentes.
- `CandleType` – Período usado para rastreamento de sessões e avaliação de sinais (padrão período de 15 minutos).

## Notas de Implementação
- Usa a API `Strategy` de alto nível do StockSharp com vinculações `SubscribeCandles` e `SubscribeOrderBook`; nenhum acesso de indicadores de baixo nível é necessário.
- `OnOwnTradeReceived` é aproveitado para manter as ordens protetoras sincronizadas com a ordem de entrada executada e para limpar quando o stop-loss ou take-profit é executado.
- A lógica de trailing deliberadamente evita chamar `GetValue` do indicador e depende apenas da vela recebida e do estado armazenado, cumprindo com as diretrizes de conversão.
- As distâncias são baseadas em passos de preço, espelhando a aritmética original baseada em pips da implementação MQL e permanecendo independente do instrumento.
- A implementação em Python é intencionalmente omitida de acordo com os requisitos da tarefa; apenas a versão em C# é fornecida nesta pasta.
