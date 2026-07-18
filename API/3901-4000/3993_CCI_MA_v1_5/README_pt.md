# CCI Estratégia MA v1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o consultor especialista MetaTrader "CCI_MA v1.5" dentro do StockSharp API de alto nível. O robô original espera que o índice de canal de commodities (CCI) cruze uma média móvel simples calculada nos próprios valores de CCI e usa um CCI secundário para supervisionar saídas em torno dos limites de ±100. A porta StockSharp mantém a mesma ordem de sinal, gerenciamento de dinheiro opcional e distâncias de parada/alvo baseadas em pontos, enquanto adapta tudo às assinaturas de velas e ligações de indicadores.

## Como funciona
* **Fonte de dados** – Uma série de velas definidas pelo usuário (velas de 15 minutos por padrão) alimenta ambos os CCIs. Os indicadores leem o preço de fechamento da vela para espelhar a configuração `PRICE_CLOSE` de MetaTrader.
* **Indicadores principais** – O `CommodityChannelIndex` primário (parâmetro `CciPeriod`) fornece a leitura do impulso. Um `SimpleMovingAverage` com período `MaPeriod` é aplicado ao fluxo de valores CCI para formar a linha de gatilho. Um CCI (`SignalCciPeriod`) secundário supervisiona reversões de sobrecompra e sobrevenda em torno de ±100.
* **Lógica de entrada** – Uma negociação longa é aberta na barra após um cruzamento ascendente: a vela concluída anteriormente (`prevCci`) deve ficar acima da média móvel CCI enquanto a vela anterior (`prev2Cci`) estava abaixo. Um sinal curto é o cruzamento simétrico para baixo. As posições opostas existentes são fechadas e invertidas adicionando o valor absoluto da posição atual ao novo tamanho da ordem, correspondendo ao comportamento da versão MQL.
* **Lógica de saída** – As posições compradas são liquidadas quando a supervisão CCI cai de acima de +100 para abaixo de +100 ou quando a primária CCI volta abaixo de sua média móvel (novamente avaliada nas duas velas finalizadas anteriormente). Os shorts saem nas condições inversas. As paradas de proteção emulam as distâncias baseadas em pontos de MetaTrader: a estratégia deriva um tamanho de pip do instrumento `PriceStep` (multiplicando por 10 para cotações de três ou cinco dígitos) e compara os extremos da vela com `entry ± distance` em cada vela concluída.
* **Dimensionamento da posição** – `LotVolume` define o tamanho base do pedido. Se `UseMoneyManagement` estiver ativado, a estratégia o multiplica por um fator inteiro igual a `floor(balance / DepositPerLot)`, limitado por `MaxMultiplier`, reproduzindo a escada de depósito do consultor especialista. O volume do pedido está alinhado com as restrições do instrumento `VolumeStep`, `MinVolume` e `MaxVolume` antes do envio.

## Parâmetros
- **Tipo de vela** – Tipo de dados de vela que alimenta todos os cálculos de indicadores.
- **CCI Período** – Duração do oscilador CCI primário.
- **Período de saída CCI** – Duração do CCI de supervisão usado para saídas de limite.
- **CCI Período MA** – Período da média móvel simples aplicada ao primário CCI.
- **Volume do lote** – Volume base de negociação antes do dimensionamento da gestão de dinheiro.
- **Ativar gerenciamento de dinheiro** – ativa o escalonamento do volume do lote com base em depósito.
- **Depósito por lote** – Incremento de saldo necessário para aumentar o multiplicador do lote em um (usado apenas quando o gerenciamento de dinheiro está ativo).
- **Max Multiplier** – Multiplicador máximo que a gestão de dinheiro pode atingir.
- **Stop Loss (pips)** – Distância em pips para o stop de proteção; definido como zero para desativar.
- **Take Profit (pips)** – Distância em pips para a meta de lucro; definido como zero para desativar.

A estratégia espera por duas velas totalmente fechadas antes de emitir a primeira ordem, para que as comparações cruzadas de duas barras correspondam exatamente à execução atrasada do especialista MQL. As verificações de stop-loss e take-profit são executadas em velas finalizadas usando seus extremos máximo/mínimo, o que se aproxima das ordens de proteção do lado do servidor de MetaTrader enquanto permanece dentro do StockSharp API de alto nível.
