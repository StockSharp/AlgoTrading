# Estratégia do Painel de Trading Manual TradeXpert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
O consultor especialista MQL5 original TradeXpert é um painel de trading operado manualmente que expõe uma coleção de botões para abrir posições, colocar ordens pendentes, aplicar stops de proteção e reverter ou fechar rapidamente uma operação existente. Este port em C# reproduz o mesmo conjunto de ferramentas dentro do StockSharp convertendo cada ação do painel em um parâmetro de estratégia. A estratégia em si não gera sinais de trading; em vez disso, ela ouve suas instruções manuais, executa as ordens solicitadas e supervisiona saídas de proteção no fluxo de velas entrantes.

## Funcionalidade Recriada
- **Ações de mercado.** Solicitações de uso único para ordens de mercado `Buy` ou `Sell` usando o volume de operação configurado.
- **Ordens pendentes.** Colocação de uma vez de ordens Buy Limit/Stop e Sell Limit/Stop usando um preço absoluto ou um deslocamento a partir do último fechamento de vela.
- **Gestão de proteção.** Os níveis de stop-loss e take-profit podem ser definidos como níveis de preço absolutos ou como deslocamentos a partir do preço de entrada registrado. A estratégia monitora os extremos das velas e fecha a posição com uma ordem de mercado quando um nível de proteção é violado.
- **Controles de saída manual.** Parâmetros dedicados replicam os botões Fechar e Reverter do painel MQL, permitindo fechar ou inverter uma posição sob demanda.

## Lógica da Estratégia
1. A estratégia assina o tipo de vela especificado por `CandleType`. O stream é usado para determinar o preço de fechamento mais recente para deslocamentos e para detectar se os níveis de proteção foram cruzados.
2. Em cada vela terminada a estratégia:
   - Aplica o último `TradeVolume` à propriedade `Volume` da classe base.
   - Trata solicitações de fechamento ou reversão manual mesmo que nenhum indicador tenha sido formado ainda.
   - Uma vez confirmados os dados de mercado como prontos, executa solicitações de entrada pendentes, registra ordens pendentes e avalia gatilhos de stop-loss / take-profit.
3. Quando o tamanho de uma posição muda (nova entrada, escalar entrada ou redução), a estratégia atualiza o preço de entrada armazenado para que os stops baseados em deslocamento reflitam imediatamente a última operação.
4. A lógica de proteção usa o máximo/mínimo das velas para identificar violações. Quando um nível é cruzado, uma ordem de mercado é enviada na direção oposta com o tamanho absoluto da posição atual para garantir que a posição seja totalmente fechada.

## Parâmetros
- **`CandleType`** – série de velas usada para monitorar preços em busca de deslocamentos e verificações de risco.
- **`TradeVolume`** – volume aplicado a cada ordem de mercado e pendente (deve ser positivo).
- **`EntryAction`** – seletor momentâneo com valores `None`, `BuyMarket` ou `SellMarket`. Definir um valor diferente de `None` dispara a ordem de mercado correspondente exatamente uma vez e depois volta para `None`.
- **`PendingAction`** – seletor de ordem pendente (`None`, `BuyLimit`, `BuyStop`, `SellLimit`, `SellStop`). A ação é consumida depois que uma ordem válida é registrada.
- **`PendingPrice`** – preço absoluto para a ordem pendente. Deixar em `0` para confiar em `PendingOffset`.
- **`PendingOffset`** – deslocamento aplicado ao fechamento de vela mais recente quando `PendingPrice` é zero. Deslocamentos positivos ajustam automaticamente o preço acima/abaixo do fechamento dependendo da ação selecionada.
- **`UseStopLoss`** / **`StopLossPrice`** / **`StopLossOffset`** – habilitar e configurar proteção de stop-loss. Os deslocamentos são medidos a partir do preço de entrada armazenado quando o preço absoluto não é fornecido.
- **`UseTakeProfit`** / **`TakeProfitPrice`** / **`TakeProfitOffset`** – configurações análogas para gestão de take-profit.
- **`ClosePositionRequest`** – definir como `true` para emitir uma saída de mercado imediata para toda a posição. O sinalizador é redefinido para `false` depois que a solicitação é processada.
- **`ReversePositionRequest`** – definir como `true` para inverter a exposição atual. A estratégia fecha a posição existente e abre uma oposta usando `ReverseVolume`, depois redefine o sinalizador.
- **`ReverseVolume`** – volume da nova posição estabelecida após uma reversão. Se precisar que o tamanho inverso corresponda à posição existente, defina-o igual à posição absoluta atual.

## Diretrizes de Uso
1. Escolha a agregação de velas (`CandleType`) que corresponda a como você quer medir deslocamentos e risco. O período padrão de 1 minuto reflete o comportamento original do painel que reagia aos ticks entrantes.
2. Configure `TradeVolume` e níveis de proteção opcionais (`StopLoss*`, `TakeProfit*`). Você pode alternar livremente entre níveis absolutos e deslocamentos; os deslocamentos são ativados sempre que o valor absoluto é deixado em zero.
3. Para ordens pendentes, decida se prefere um preço fixo (`PendingPrice`) ou um deslocamento do último fechamento (`PendingOffset`). A estratégia recalcula o preço no momento em que a ordem é enviada.
4. Envie instruções de operação alterando `EntryAction`, `PendingAction`, `ClosePositionRequest` ou `ReversePositionRequest`. Cada parâmetro se comporta como um botão: uma vez executada a solicitação, o valor é automaticamente redefinido para que a ação não se repita na próxima vela.
5. A estratégia continua monitorando a ação do preço enquanto uma posição está aberta. Sempre que um limiar de stop-loss ou take-profit é cruzado, a posição é fechada com uma ordem de mercado; ambos os gatilhos de proteção são desabilitados até a próxima entrada para evitar ordens duplicadas.

## Diferenças da Versão MQL Original
- O painel visual é substituído por parâmetros de estratégia. Cada botão da UI original agora está exposto como um interruptor ou seletor que pode ser editado a partir da grade de parâmetros do StockSharp ou scripts de automação.
- Em vez de colocar ordens stop ou limite para proteção, a estratégia fecha a posição com ordens de mercado quando os níveis de preço especificados são violados. Isso mantém a implementação compatível com a API de alto nível e evita a manutenção de ordens stop separadas.
- Os deslocamentos de preço usam velas terminadas em vez de ticks brutos. Isso mantém o comportamento determinístico em backtests e sessões de trading ao vivo enquanto ainda entrega responsividade intradiária.

## Notas
- Você pode enfileirar múltiplas instruções dentro da mesma vela (por exemplo, solicitar uma compra de mercado e imediatamente solicitar um deslocamento de take-profit). A estratégia os processa sequencialmente na próxima vela terminada.
- Se precisar reemitir a mesma ação, basta selecionar o valor desejado novamente; a lógica de rastreamento interno detecta a mudança e executa a nova solicitação.
- Ao escalar para uma posição, o preço de entrada armazenado é atualizado para o fechamento da vela que reflete o novo tamanho. Ajuste os deslocamentos de acordo se precisar de distâncias de proteção precisas.
