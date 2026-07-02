# Estratégia RSI Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
RSI Martingale é uma versão do MetaTrader 5 consultor especialista `RSI&Martingale1.5`. A estratégia procura reversões de impulso aguardando até que o Índice de Força Relativa (RSI) atinja um valor extremo dentro de uma janela de lookback configurável. Quando um extremo aparece, ele abre uma negociação na direção da reversão média esperada e sai quando RSI cruza a linha média 50 ou quando uma meta fixa de stop/take é atingida. Um módulo martingale pode opcionalmente reabrir a posição na direção oposta com um volume aumentado após uma negociação perdida. Os limites diários de lucros e perdas, juntamente com filtros horários, tornam possível suspender a negociação durante sessões mais arriscadas ou após cumprir as metas de preservação de capital.

## Lógica estratégica
### RSI extremos
* **Indicador** – um único RSI calculado no tipo de vela selecionado. O indicador deve ser formado (dados históricos suficientes) antes que as negociações sejam consideradas.
* **Detecção mínima** – se o valor RSI mais recente for menor ou igual a cada valor RSI dentro da janela `Bars For Extremes` configurada e o valor estiver abaixo de 50, a estratégia abre uma posição longa.
* **Detecção máxima** – se o valor RSI mais recente for maior ou igual a todos os valores dentro da janela de lookback e o valor estiver acima de 50, a estratégia abre uma posição curta.

### Gestão de posição
* **Gatilho de saída** – as posições são fechadas quando RSI cruza a linha neutra 50 para o lado oposto (comprados saem acima de 50, shorts saem abaixo de 50).
* **Metas fixas** – distâncias opcionais de stop-loss e take-profit expressas em pips. Quando ativada, a estratégia compara a máxima/mínima da vela mais recente com os preços-alvo e fecha a posição se qualquer um dos níveis for atingido.
* **Alinhamento de volume** – cada volume de pedido é alinhado à etapa de segurança, configurações mínimas e máximas antes do envio.

### Martingale recuperação
* **Gatilho** – após o fechamento de uma posição com lucro negativo, a estratégia lembra a direção e o volume da negociação perdedora.
* **Reentrada** – na próxima vela elegível, e somente se nenhuma posição estiver aberta, ela poderá abrir imediatamente uma negociação na direção oposta. O volume é o volume perdido multiplicado por `Martingale Multiplier` ou pela base `Initial Volume` dependendo da opção `Enable Martingale`.
* **Redefinir** – assim que o pedido de martingale for enviado, as informações de perda armazenadas serão apagadas para evitar tentativas repetidas.

### Controle diário de capital
* **Linha de base** – a estratégia captura o patrimônio da conta no início de cada dia de negociação e redefine o sinalizador de suspensão.
* **Janela de monitoramento** – os limites diários são avaliados somente entre `Daily Control Start` e `Daily Control End` horas.
* **Suspensão** – se o patrimônio crescer além de `Daily Profit %` ou cair abaixo de `Daily Loss %`, a estratégia fecha qualquer posição aberta e pula novas negociações até o dia seguinte.

### Filtros de sessão
* **Janela de negociação** – novas posições são permitidas somente quando a hora atual estiver entre `Trading Start` e `Trading End` (inclusive).
* **Evitação de horas** – 24 parâmetros booleanos refletem as configurações de “evitar notícias” da fonte EA e bloqueiam a negociação durante as horas selecionadas.

## Parâmetros
* **Volume Inicial** – volume base do pedido para lançamentos padrão.
* **RSI Período** – número de períodos usados pelo indicador RSI.
* **Barras para extremos** – quantas velas finalizadas são verificadas ao procurar o mínimo ou máximo RSI mais recente.
* **Take Profit (pips)** – distância até o take-profit fixo; defina como `0` para desativar.
* **Stop Loss (pips)** – distância até o stop-loss fixo; defina como `0` para desativar.
* **Ativar Martingale** – ativa o módulo de recuperação martingale após uma negociação perdida.
* **Martingale Multiplicador** – multiplicador aplicado ao volume perdido anterior quando o martingale está ativo.
* **Metas Diárias** – alterna a lógica diária de suspensão de lucros/perdas.
* **% de lucro diário** – porcentagem de lucro que interrompe a negociação no dia atual.
* **% de perda diária** – porcentagem de perda que interrompe a negociação no dia atual.
* **Início do Controle Diário/Fim do Controle Diário** – limites de horas para avaliar os limites diários.
* **Início / Fim da Negociação** – limites de horas que permitem novas posições.
* **Evitar a hora 00… Evitar a hora 23** – desativa a negociação durante a hora correspondente.
* **Tipo de vela** – assinatura de vela usada para o indicador RSI e todos os cálculos.

## Notas adicionais
* A estratégia opera apenas em velas finalizadas e não avalia ticks intrabarras.
* Os cálculos de lucro diário combinam o PnL da estratégia realizada com o PnL flutuante com base no último preço de fechamento.
* Não há implementação Python para esta estratégia no pacote; apenas a versão C# é fornecida.
