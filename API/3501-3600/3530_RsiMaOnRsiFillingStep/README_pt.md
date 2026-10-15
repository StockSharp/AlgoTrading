# RSI MA em RSI Estratégia de Etapas de Preenchimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **RSI MA em RSI Filling Step Strategy** é uma versão StockSharp do MetaTrader consultor especialista `RSI_MAonRSI_Filling Step EA.mq5`. O sistema original mede o impulso com um Índice de Força Relativa (RSI) e suaviza esse oscilador com uma média móvel. As negociações são iniciadas quando RSI cruza sua média móvel, enquanto ambos os valores permanecem no mesmo lado do nível 50 intermediário. A conversão mantém os filtros de direção de negociação configuráveis, o temporizador de sessão opcional e a capacidade de reverter os sinais enquanto aproveita as ligações de indicadores de alto nível de StockSharp.

## Lógica de negociação
1. Assine a série de velas selecionada e calcule dois indicadores em cada barra finalizada: `RelativeStrengthIndex` com comprimento `RsiPeriod` e `MovingAverage` (`MaType`, `MaPeriod`) aplicados ao fluxo RSI.
2. Aguarde as velas completas antes de agir, replicando a salvaguarda da "nova barra" do EA para que cada barra produza no máximo uma decisão de negociação.
3. Uma configuração de **alta** ocorre quando o valor anterior de RSI estava abaixo de sua média móvel e o valor mais recente fecha acima da média enquanto ambas as leituras permanecem abaixo de `MiddleLevel` (padrão 50). Uma configuração **baixa** é o caso espelhado acima do nível médio.
4. Quando `ReverseSignals` está ativado, a condição de alta gera uma negociação curta e a condição de baixa gera uma negociação longa, imitando a sinalização reversa de EA.
5. O parâmetro `Mode` limita a negociação apenas para compra, venda ou ambas as direções. Guardas adicionais opcionalmente fecham a exposição oposta e bloqueiam novas entradas quando uma posição já está aberta.
6. Uma janela de tempo diária idêntica à implementação MetaTrader pode desativar sinais fora do intervalo `SessionStart` → `SessionEnd` configurado, incluindo sessões que terminam à meia-noite.

## Parâmetros
- **CandleType** – série de dados processados pela estratégia. O padrão são velas com intervalo de uma hora.
- **RsiPeriod** – duração média de RSI (padrão 14).
- **MaPeriod** – duração da média móvel aplicada a RSI (padrão 21).
- **MaType** – tipo de média móvel usado para suavização RSI (padrão `Simple`).
- **MiddleLevel** – nível central RSI usado por ambos os indicadores para validar negociações (padrão 50).
- **ReverseSignals** – inverte a interpretação do cruzamento de alta/baixa (padrão `false`).
- **Modo** – filtro de direção comercial (`BuyOnly`, `SellOnly`, `Both`).
- **CloseOppositePositions** – se deve nivelar a posição oposta antes de entrar em uma nova negociação (padrão `false`).
- **OnlyOnePosition** – evita novas ordens enquanto uma posição já estiver aberta (padrão `false`).
- **UseTimeWindow** – habilita o filtro diário da sessão de negociação (padrão `false`).
- **SessionStart / SessionEnd** – horários de início e término do pregão permitido. Funciona com sessões noturnas encerrando depois da meia-noite.

## Notas de implementação
- Os valores do indicador são entregues por meio de `Bind`, eliminando a necessidade de gerenciamento manual de buffer que o EA original exigia com `CopyBuffer`.
- Os valores anteriores de RSI e de média móvel são armazenados em cache para espelhar o padrão de acesso `RSI[m_bar_current+1]` de MQL. O campo `_lastSignalBarTime` garante apenas uma negociação por barra, assim como os carimbos de data e hora `m_last_deal_buy_in` / `m_last_deal_sell_in` do EA.
- O gerenciamento comercial usa `BuyMarket()` e `SellMarket()` para espelhar a execução imediata de mercado do EA. O fechamento opcional da exposição oposta é feito com `ClosePosition()` antes de fazer o novo pedido.
- O filtro de tempo replica a função `TimeControlHourMinute` do EA, incluindo a lógica da janela noturna em que o horário de início é maior que o horário de término.
- Os ajudantes de gráficos desenham velas de preços com marcadores comerciais, além de um painel RSI dedicado para que os cruzamentos possam ser inspecionados visualmente durante os backtests.

## Diferenças em comparação com o Expert Advisor
- As opções de gerenciamento de dinheiro (`ENUM_LOT_OR_RISK`, trailing stops, verificações de nível de congelamento) não são reproduzidas. Os usuários do StockSharp podem anexar sua própria lógica de proteção ou módulos de risco.
- Confirmações de negociação, verificações de números mágicos e filas manuais de pedidos do EA são desnecessárias porque o StockSharp gerencia os ciclos de vida dos pedidos de maneira diferente. A estratégia pressupõe disponibilidade imediata de ordens de mercado.
- As ordens stop-loss e take-profit não são anexadas automaticamente. Use `StartProtection` ou módulos externos se esse comportamento for necessário.

## Dicas de uso
1. Mantenha `MiddleLevel` próximo de 50 para permanecer fiel ao comportamento original de reversão à média. Desviar-se desse valor empurra o sistema para uma negociação de ruptura.
2. Ative `OnlyOnePosition` se preferir transições rigorosas de horizontal para posição. Desative-o para permitir a pirâmide com lógica de volume personalizada.
3. Combine o filtro de tempo com o horário de negociação da bolsa ao operar futuros ou ações para evitar ruídos noturnos.
4. Otimize `MaPeriod`, `RsiPeriod` e `MiddleLevel` juntos ao adaptar a estratégia a novos instrumentos.

Com essas notas, você pode executar, personalizar e estender com segurança a estratégia RSI MA em RSI Filling Step dentro do ambiente StockSharp.
