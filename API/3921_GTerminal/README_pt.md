# Estratégia GTerminal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia GTerminal é uma porta C# do MetaTrader 4 consultor especialista `GTerminal_V5a`. O script original permitia manual
controle de entradas e saídas traçando linhas horizontais no gráfico. Esta porta recria o mesmo comportamento orientado por linha dentro
a estrutura StockSharp expondo cada linha virtual como um parâmetro configurável. Sempre que o preço de fechamento do ativo selecionado
série de velas cruza uma dessas linhas virtuais, a estratégia abre, fecha ou reverte posições da mesma forma que MQL4
versão. Níveis de proteção automática opcionais emulam as linhas auxiliares "tpinit" e "slinit" da ferramenta original.

## Lógica estratégica
### Amostragem de preços
* A estratégia funciona em velas finalizadas de um período definido pelo usuário (`CandleType`).
* `StartShift` controla qual vela é usada como fechamento de referência. Um valor de `0` usa o fechamento atual da vela, `1` usa o
vela anterior, etc. A mudança também afeta a vela de comparação, então o script sempre avalia dois fechamentos consecutivos como o
MetaTrader implementação.
* `CrossMethod` espelha a entrada MQL4:
  * `0` – cruzamento estrito: o fechamento anterior deve estar abaixo (para gatilhos longos) ou acima (para gatilhos curtos) do nível e do
o fechamento atual deve terminar no lado oposto do nível.
  * `1` – gatilho instantâneo: o fechamento atual só precisa estar acima/abaixo do nível. A porta ainda verifica o fechamento anterior de
evita vários gatilhos na mesma barra, replicando o comportamento “toque uma vez” obtido em MetaTrader excluindo a linha
depois de disparar.

### Regras de entrada
* **Linha Stop de compra** – quando o fechamento se move de baixo para cima `BuyStopLevel`, a estratégia compra. Se uma posição curta estiver aberta,
o tamanho do pedido inclui o volume necessário para nivelar a posição curta mais o `Volume` configurado para a nova exposição longa.
* **Linha Buy Limit** – quando o fechamento cai até `BuyLimitLevel`, uma posição longa é aberta usando a mesma lógica de volume.
* **Linha Sell Stop** – quando o fechamento se move de cima para baixo `SellStopLevel`, a estratégia vende. As posições compradas existentes são fechadas como
parte da quantidade do pedido.
* **Linha Sell Limit** – quando o fechamento sobe até `SellLimitLevel`, uma posição curta é aberta.
* As entradas são ignoradas quando `Volume` é `0` ou `PauseTrading` está ativado.

### Regras de saída
* **Saídas direcionais** – `LongStopLevel` e `LongTakeProfitLevel` fecham o lado longo quando o fechamento cruza o respectivo
linha. `ShortStopLevel` e `ShortTakeProfitLevel` fazem o mesmo para exposições curtas.
* **Saídas globais** – `AllLongStopLevel` / `AllLongTakeProfitLevel` liquidam todas as posições longas, independentemente de como foram abertas.
`AllShortStopLevel` / `AllShortTakeProfitLevel` espelham a lógica para shorts.
* **Proteção inicial** – definir `UseInitialProtection` como `true` aplica o `InitialLongStopLevel`, `InitialLongTakeProfitLevel`,
`InitialShortStopLevel` e `InitialShortTakeProfitLevel` imediatamente após o preenchimento de uma nova posição. Esses níveis se comportam como
linhas auxiliares "slinit" / "tpinit" do script original e permanecem ativas até que a posição seja fechada ou o nível seja atualizado.
* Apenas uma ação de saída é enviada por vela. Quando uma condição de saída é atendida, a estratégia envia a ordem de fechamento e ignora a
verificações restantes para essa barra, assim como a versão MQL4 parou após a linha ser disparada.

### Controle de pausa
* `PauseTrading` reproduz a funcionalidade da linha MetaTrader "PAUSE". Quando ativado, nenhuma lógica de entrada ou saída é avaliada.
O estado pode ser alternado manualmente sem recarregar a estratégia.

## Parâmetros
* **Volume** – volume de pedidos para novas entradas. O tamanho final do pedido inclui automaticamente qualquer exposição oposta que deva ser
fechado durante uma reversão.
* **Método cruzado** – selecione o algoritmo de cruzamento (`0` estrito, `1` instantâneo).
* **Start Shift** – deslocamento da vela usada para o cálculo do cruzamento.
* **Pausar negociação** – desativa todas as ações de negociação enquanto `true`.
* **Usar proteção inicial** – permite a aplicação automática dos níveis iniciais de stop/take-profit após cada preenchimento.
* **Buy Stop Level / Buy Limit Level** – níveis de preços que acionam entradas longas.
* **Sell Stop Level / Sell Limit Level** – níveis de preços que acionam entradas curtas.
* **Long Stop Level / Long Take Profit** – linhas de saída para a posição longa ativa.
* **Short Stop Level / Short Take Profit** – linhas de saída para a posição curta ativa.
* **All Long Stop / All Long Take Profit** – linhas de saída globais que fecham todas as posições longas.
* **All Short Stop / All Short Take Profit** – linhas de saída globais que fecham todas as posições vendidas.
* **Initial Long Stop / Initial Long Take Profit** – níveis de proteção ativados após cada entrada longa quando a proteção inicial é
habilitado.
* **Initial Short Stop / Initial Short Take Profit** – níveis de proteção ativados após cada entrada curta quando a proteção inicial é
habilitado.
* **Tipo de Candle** – intervalo de tempo que fornece os preços de fechamento utilizados para comparações.

## Notas de implementação
* A porta mantém o fluxo de trabalho baseado em linhas, mas expõe cada linha como um parâmetro em vez de depender de objetos gráficos. Os usuários podem
atualizar os níveis dinamicamente através da grade de parâmetros, imitando a maneira como as linhas foram movidas em um gráfico MetaTrader.
* Os gatilhos da janela do indicador do script original (RSI, CCI, Momentum, etc.) não estão disponíveis nesta versão. Todos os gatilhos
use apenas preços de fechamento. O conjunto de parâmetros ainda pode ser combinado com outros componentes StockSharp se o comportamento for orientado por indicadores
é necessário.
* A estratégia depende exclusivamente de ordens de mercado (`BuyMarket`, `SellMarket`) assim como o script MQL4, que usou ordens de mercado para
emular execução de linha pendente.
* Não há implementação em Python; apenas a versão C# é fornecida neste pacote.
