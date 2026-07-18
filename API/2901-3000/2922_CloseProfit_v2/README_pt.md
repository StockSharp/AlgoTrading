# Estratégia CloseProfit V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
CloseProfit V2 replica o comportamento do utilitário original do MetaTrader que força o fechamento de toda a exposição de trading ativa assim que um limite configurável de lucro ou perda é atingido. O port do StockSharp atua como um módulo de proteção de conta: monitora o PnL flutuante em cada vela concluída e, quando os limites são excedidos, cancela ordens pendentes e liquida posições. A estratégia é projetada para ser executada junto com entradas discricionárias ou automatizadas que dependem do mesmo portfólio.

Ao contrário dos sistemas geradores de sinais, CloseProfit V2 nunca abre posições por conta própria. Ela simplesmente observa métricas de lucro e perda em tempo real, permitindo que os traders automatizem a lógica do "botão de pânico" usada na versão MQL. A frequência de monitoramento é controlada por meio de uma assinatura de velas, o que torna o componente compatível tanto com backtesting histórico quanto com ambientes de trading ao vivo.

## Como funciona
1. Quando a estratégia começa, ela captura o valor atual do portfólio como o último snapshot de patrimônio em posição plana e lança a assinatura de velas configurada.
2. Cada vez que uma vela termina, a estratégia armazena o preço de fechamento e avalia o lucro flutuante:
   - Se `AllSymbols` estiver desativado, apenas o instrumento principal é rastreado. O lucro flutuante é calculado como `Position * (lastClose - averagePrice)`, portanto apenas o PnL não realizado é usado, refletindo a lógica MQL que soma as negociações abertas.
   - Se `AllSymbols` estiver ativado, o módulo compara o valor atual do portfólio com o último snapshot de patrimônio em posição plana. Isso mede o ganho/perda não realizado combinado em todos os instrumentos gerenciados pela estratégia.
3. Quando o lucro flutuante excede `ProfitClose` ou cai abaixo de `-LossClose`, a estratégia solicita uma liquidação completa. Ela cancela imediatamente ordens ativas e envia instruções de mercado para zerar cada instrumento afetado.
4. Após a liquidação ser concluída e todas as posições chegarem a zero, o snapshot de patrimônio em posição plana é atualizado. Isso garante que o monitoramento subsequente comece do novo saldo da conta e evita re-acionamento em lucros realizados.

A implementação reflete o comportamento do EA MQL original: ignora o PnL histórico realizado e reage puramente a posições abertas. Um bloco de proteção integrado garante que a rotina de fechamento seja executada apenas uma vez por sinal e não envie spam de solicitações de cancelamento.

## Parâmetros
- **ProfitClose (padrão 10)** – Limite de lucro flutuante na moeda da conta. Quando os ganhos não realizados atingem esse nível, a estratégia liquida todas as posições monitoradas.
- **LossClose (padrão 1000)** – Limite de perda flutuante. Uma vez que o drawdown não realizado excede esse valor absoluto, todas as posições são fechadas para interromper perdas adicionais.
- **AllSymbols (padrão false)** – Se `false`, apenas o `Security` principal atribuído à estratégia é observado. Se `true`, o módulo agrega o PnL flutuante de todos os instrumentos no conjunto de posições da estratégia e os liquida todos simultaneamente.
- **CandleType (padrão período de 1 minuto)** – Série de velas usada para avaliação. O preço de fechamento da vela impulsiona os cálculos de lucro quando `AllSymbols` está desativado. Um período mais curto proporciona reações mais rápidas, enquanto períodos mais longos reduzem a carga computacional durante os backtests.

## Notas práticas
- Inicie o componente junto com outras estratégias de trading que compartilham o mesmo portfólio. Uma vez atingidos os limites, CloseProfit V2 cancelará suas ordens pendentes e fechará suas posições abertas.
- Ajustes de comissão e swap não estão disponíveis na API de alto nível do StockSharp, portanto o PnL flutuante é baseado puramente em diferenças de preço. Se esses custos importam, aumente os limites adequadamente.
- Como a liquidação depende de ordens de mercado, certifique-se de que há liquidez suficiente ou buffers de slippage ao configurar `ProfitClose` e `LossClose`.
- A assinatura de velas também é usada durante o backtesting para garantir pontos de avaliação determinísticos. No trading ao vivo, você pode mudar para períodos mais rápidos se o monitoramento intra-barra for necessário.
- A estratégia chama `StartProtection()` na inicialização para que as verificações de segurança integradas do StockSharp (por exemplo, tratamento de reconexão) permaneçam ativas enquanto o utilitário está em execução.

## Diferenças da implementação MQL original
- O filtro de "magic number" do MetaTrader é desnecessário: o StockSharp identifica ordens por estratégia, portanto o módulo já isola as posições que controla. `AllSymbols` portanto se aplica a todos os instrumentos gerenciados pela mesma instância de estratégia.
- O EA MQL gerenciava rótulos de gráfico para exibir saldo, patrimônio e contagens de tickets. A versão C# usa mensagens de log porque o charting do StockSharp é opcional e nem sempre está disponível em execuções automatizadas.
- O scaffolding de depuração/tester que criava automaticamente negociações de demonstração no MQL foi removido. A estratégia StockSharp foca puramente em monitoramento e liquidação.

## Quando usar
Implante CloseProfit V2 sempre que um stop rígido no PnL flutuante for necessário—seja para proteger contas financiadas, fazer cumprir políticas de risco proprietárias, ou automatizar metas de lucro baseadas em sessão. Ajuste o período de velas para alinhar com a velocidade de reação exigida pelo seu fluxo de trabalho de trading.
