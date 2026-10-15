# Estratégia do Colibri Grid Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Colibri Grid Manager é uma versão StockSharp do MetaTrader 4 consultor especialista `Colibri.mq4` (pasta original `MQL/9713`). A estratégia concentra-se na negociação de grade discricionária: ela prepara ordens pendentes em camadas sob demanda, dimensiona cada ordem usando o orçamento de risco configurado, anexa saídas de proteção e impõe um limite de saque diário antes de desabilitar negociações adicionais.

## Lógica de negociação
1. Quando a estratégia é iniciada, ela assina a série de velas selecionada e o livro de pedidos para acompanhar os preços de referência, zera a base de lucro diário e limpa os pedidos anteriores.
2. Se `EnableGrid` for verdadeiro e não existir posição ou ordem de grade ativa, a estratégia constrói uma nova grade para cada direção permitida (`AllowBuy`, `AllowSell`). As ordens podem ser distribuídas em torno de um preço central manual ou em relação a âncoras de entrada de compra/venda explícitas.
3. O tipo de ordem (`OrderType`) controla se a grade usa entradas de mercado limitadas, stop ou imediatas. A distância entre os níveis é definida em pontos via `LevelSpacingPoints` e convertida em incrementos de preço usando o tamanho do tick do instrumento.
4. O volume é fixo (`FixedOrderVolume`) ou derivado de `RiskPercent`. O dimensionamento baseado no risco aloca a percentagem configurada do capital atual da carteira em todos os níveis numa direção e divide-a pelo risco monetário implícito no stop protetor.
5. Depois que uma ordem de entrada é preenchida, a estratégia coloca automaticamente ordens de proteção emparelhadas: as paradas são derivadas de `StopLossPrice` ou `StopDistancePoints`, enquanto os lucros dependem de `TakeProfitDistancePoints` ou o padrão é um passo da grade de distância. Pedidos pendentes podem expirar após `ExpirationHours` horas.
6. A estratégia monitora continuamente o PnL realizado e flutuante. Se a perda do dia de negociação atual violar `DailyLossLimitPercent`, todas as ordens serão canceladas, as posições abertas serão fechadas e a criação de uma nova grade será suspensa até o início do dia seguinte.
7. Alternadores manuais (`CloseAllPositions`, `CloseLongPositions`, `CloseShortPositions`, `CancelOrders`) permitem que o trader nivele ou limpe o livro instantaneamente sem tocar no código.

## Parâmetros
- **EnableGrid** – switch mestre que ativa ou desativa a manutenção automática da rede.
- **OrderType** – tipo de pedido de entrada (`Limit`, `Stop`, `Market`) usado ao criar níveis.
- **AllowBuy / AllowSell** – escolha os lados que poderão participar da grade.
- **UseCenterLine / CenterPrice** – quando habilitado, distribui os níveis de compra/venda simetricamente em torno de um preço central; um centro zero usa o preço médio.
- **LevelSpacingPoints** – espaçamento entre níveis consecutivos, medido em pontos e convertido em diferenças absolutas de preço por meio do tamanho do tick do instrumento.
- **LevelsCount** – número de níveis por direção. Para a modalidade mercado apenas uma ordem é enviada independente deste valor.
- **BuyEntryPrice / SellEntryPrice** – âncoras explícitas para grades longas e curtas quando o modo central está desabilitado (o padrão é zero para o lance/venda atual).
- **StopLossPrice** – nível de stop absoluto aplicado a cada ordem. Deixe zero para derivar a parada de `StopDistancePoints`.
- **StopDistancePoints** – distância de parada substituta em pontos quando nenhum preço de parada absoluto é fornecido.
- **TakeProfitDistancePoints** – distância de take-profit opcional em pontos. Quando zero, a estratégia usa uma etapa da grade como meta padrão.
- **UseRiskSizing / RiskPercent** – permite o dimensionamento baseado em porcentagem e define a parcela do patrimônio alocada para cada grade direcional. O valor é dividido igualmente em todos os níveis dessa direção.
- **FixedOrderVolume** – tamanho do pedido usado quando o dimensionamento baseado em risco está desabilitado ou não produz um volume válido.
- **ExpirationHours** – vida útil opcional para pedidos de grade pendentes.
- **DailyLossLimitPercent** – limite de stop-trading expresso como uma fração do patrimônio do portfólio capturado no início do dia de negociação.
- **CloseAllPositions / CloseLongPositions / CloseShortPositions / CancelOrders** – comandos de manutenção manual acessíveis na IU.
- **CandleType** – série de velas usadas para eventos de manutenção, como reinicializações diárias.

## Notas de implementação
- A estratégia depende exclusivamente do StockSharp API de alto nível: `SubscribeCandles`, `SubscribeOrderBook`, `BuyLimit`, `SellStop`, etc. Nenhuma lógica direta do conector ou acesso ao indicador é necessária.
- O dimensionamento do pedido usa `Security.PriceStep` e `Security.StepPrice` para traduzir distâncias baseadas em pontos do script MQL em risco monetário.
- As saídas de proteção são implementadas por meio de ordens de parada/limite separadas, em vez de modificar a ordem de entrada original, que corresponde à maneira como StockSharp lida com ordens de proteção vinculadas.
- O filtro de perdas diárias é redefinido quando o dia do calendário muda e o valor do portfólio é registrado novamente. Os traders podem retomar a negociação manualmente alternando `EnableGrid` se quiserem anular o bloqueio de segurança.
- Variáveis globais MT4, sinalizadores de fechamento de emergência e rotinas de limpeza gráfica do script de origem foram substituídos por parâmetros fortemente digitados e alternâncias manuais.

## Dicas de uso
1. Defina se a rede deve ser centrada ou ancorada em preços específicos antes de ativá-la. Para grades centralizadas, forneça um `CenterPrice` significativo; para grades ancoradas deixe desabilitado e preencha os preços de entrada de compra/venda.
2. Calibre `LevelSpacingPoints`, `StopDistancePoints` e `TakeProfitDistancePoints` para corresponder à volatilidade do instrumento. Lembre-se de que todos os três são valores baseados em pontos.
3. Ao usar o dimensionamento baseado em risco, verifique se o instrumento possui `PriceStep` e `StepPrice` válidos; caso contrário, a estratégia voltará ao volume fixo.
4. Use os parâmetros de controle manual para cancelar ou nivelar posições rapidamente antes de modificar os parâmetros de configuração.
5. Combine o limite diário de perdas com a gestão de risco externo se várias estratégias partilharem a mesma carteira.

## Diferenças em relação ao Expert Advisor original
- A versão StockSharp concentra-se em uma interface de parâmetros limpa em vez de variáveis globais MT4 e lógica de número mágico baseada em comentários.
- Sinalizadores de fechamento de emergência, ajustes automáticos de tamanho de grade e limpeza de objetos gráficos do código original são reduzidos a alternâncias manuais e validação direta de parâmetros.
- Os auxiliares de parada final do script MQL não são replicados; use os módulos finais existentes de StockSharp, se necessário.
- A lógica de dependência MQL entre pedidos (executar/cancelar com base em pedidos “mãe”) não é reproduzida. Cada nível opera de forma independente com as suas próprias ordens de proteção.

Esses ajustes mantêm o espírito do consultor especialista Colibri original – entradas estruturadas em vários níveis com gerenciamento rigoroso de dinheiro – enquanto alinham a implementação com padrões idiomáticos StockSharp.
