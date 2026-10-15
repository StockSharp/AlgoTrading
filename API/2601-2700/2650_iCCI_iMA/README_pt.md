# Estratégia iCCI iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista MetaTrader «iCCI iMA». Opera cruzamentos do Commodity Channel Index (CCI) contra uma média móvel exponencial (EMA) aplicada diretamente ao fluxo do CCI. Um CCI secundário, calculado com seu próprio período, supervisiona reversões de sobrecompra/sobrevenda ao redor das bandas ±100. As ordens são dimensionadas em lotes, opcionalmente escaladas pelo saldo da conta, e cada operação é protegida por níveis configuráveis de stop-loss e take-profit expressos em pips.

## Como funciona
* **Fonte de dados** – Uma série de velas configurável (velas de 1 minuto por padrão) alimenta todos os cálculos de indicadores usando o preço típico da vela `(high + low + close) / 3`.
* **Indicadores principais** – O CCI primário mede o momentum com o comprimento `CciPeriod`. Uma EMA desse CCI (comprimento `MaPeriod`) suaviza o oscilador e atua como linha de sinal. O CCI secundário `CciClosePeriod` monitora cruzamentos de limiar.
* **Lógica de entrada** – Uma posição comprada é aberta quando o CCI atual está acima de sua EMA enquanto o valor de duas velas completadas atrás estava abaixo da EMA, indicando um cruzamento ascendente. Uma posição vendida espelha esta lógica quando o CCI cruza para baixo. O algoritmo só opera depois que todos os indicadores estiverem completamente formados e duas barras históricas estiverem disponíveis para reproduzir o look-back original da implementação MQL.
* **Lógica de saída** – Comprados existentes fecham quando o CCI secundário cai de volta abaixo de +100 ou quando o CCI primário cai abaixo da EMA após ter estado acima duas barras antes. Vendidos saem quando o CCI secundário sobe acima de −100 ou quando o CCI sobe de volta acima da EMA sob a mesma confirmação de duas barras. Stops protetores monitoram cada vela finalizada: posições compradas fecham se o preço for para `entry − stopLossPips * pipSize` e realizam lucro em `entry + takeProfitPips * pipSize`; vendidos usam os níveis simétricos com `entry + stopLoss` e `entry − takeProfit`. O tamanho do pip é derivado do passo de preço do ativo e se adapta a cotações de 3 ou 5 dígitos multiplicando o tamanho do tick por 10, correspondendo à conversão do MetaTrader.
* **Dimensionamento de posição** – O tamanho de lote base (`LotSize`) é validado contra os valores `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para que as ordens respeitem as restrições da bolsa. Se o gerenciamento de dinheiro estiver habilitado, a estratégia multiplica o tamanho de lote por um fator inteiro igual ao saldo da conta dividido por `DepositPerLot`, limitado a 20, e atualizado em cada barra, reproduzindo o escalonamento inteiro do especialista original.

## Parâmetros
- **Tipo de Vela** – Série de dados usada para cálculos de indicadores.
- **Período CCI** – Comprimento do CCI primário que impulsiona os sinais de cruzamento.
- **Período CCI Fechamento** – Comprimento do CCI secundário para monitorar reversões ±100.
- **Período EMA CCI** – Período da EMA que suaviza os valores do CCI primário.
- **Tamanho do Lote** – Volume de trading base em lotes antes de qualquer escalonamento.
- **Habilitar Gerenciamento de Dinheiro** – Ativa o escalonamento do tamanho de lote baseado em saldo.
- **Depósito Por Lote** – Incremento de saldo necessário para aumentar o multiplicador de lote em um (ativo apenas quando o gerenciamento de dinheiro está ativado).
- **Stop-Loss (pips)** – Distância de stop protetor em pips; definir como zero para desabilitar.
- **Take-Profit (pips)** – Distância do alvo de lucro em pips; definir como zero para desabilitar.

O algoritmo requer duas velas completamente terminadas antes de começar a operar para que as comparações de cruzamento de duas barras correspondam à lógica MQL fonte. As verificações de stop-loss e take-profit são avaliadas em velas fechadas usando seus extremos de máxima/mínima, o que aproxima as ordens protetoras do lado do servidor MetaTrader dentro da API StockSharp de alto nível.
