# Coelho M3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Rabbit M3 é uma versão do MetaTrader 4 Expert Advisor `RabbitM3` (também lançado sob o nome "Petes Party Trick"). O sistema alterna entre regimes longos e apenas curtos usando um par de médias móveis exponenciais horárias. A confirmação do impulso vem de um cruzamento Williams %R combinado com um filtro de nível CCI, enquanto um canal Donchian extremamente longo observa quebras de preços que invalidam o viés da tendência atual. O tamanho da posição pode opcionalmente aumentar após grandes vencedores, replicando a regra de escala de lote contida no código original.

## Lógica estratégica
### Filtro de regime de tendência
* Quando o EMA rápido fecha abaixo do EMA lento, qualquer exposição longa existente é liquidada e novos sinais são restritos ao lado curto.
* Quando o EMA rápido fecha acima do EMA lento, qualquer exposição curta existente é fechada e apenas as configurações longas permanecem elegíveis.
* Se as EMAs forem iguais, o regime anterior é mantido, espelhando a lógica MetaTrader que apenas alterna em desigualdades estritas.

### Regras de entrada
* **Negociações curtas**
  * O regime deve ser apenas curto (EMA rápida abaixo do EMA lenta).
  * Williams %R (comprimento = `WilliamsPeriod`) deve cruzar o `WilliamsSellLevel` na vela mais recente enquanto o valor anterior ainda estava abaixo de zero.
  * CCI (comprimento = `CciPeriod`) deve ser maior ou igual a `CciSellLevel`.
  * A posição líquida deve ser plana; a estratégia abre no máximo `MaxOpenPositions` negociações e o padrão é uma única ordem de mercado de tamanho `EntryVolume`.
* **Negociações longas**
  * O regime deve ser apenas longo (EMA rápida acima do EMA lenta).
  * Williams %R deve cruzar `WilliamsBuyLevel` enquanto o valor anterior ainda estava abaixo de zero.
  * CCI deve ser menor ou igual a `CciBuyLevel`.
  * A posição líquida deve ser estável antes que uma nova compra seja iniciada.

### Regras de saída
* **Paradas bruscas** – `StopLossPips` e `TakeProfitPips` são convertidos em compensações de preço usando a etapa de preço do instrumento. Um valor de `0` desativa o nível de proteção correspondente.
* **Donchian rompimento** – se o preço fechar acima da banda superior anterior Donchian (comprimento = `DonchianLength`) qualquer posição curta será fechada imediatamente. Um fechamento abaixo da banda inferior anterior fecha posições compradas. O canal usa o valor concluído anteriormente para reproduzir o atraso `iHighest`/`iLowest` do EA.
* **Inversão de regime** – sempre que o relacionamento EMA reverte, a estratégia liquida a exposição oposta antes de permitir novas negociações na nova direção.

### Gestão de dinheiro
* Começa com `EntryVolume` unidades por negociação.
* Quando ocorre um lucro realizado superior a `BigWinThreshold` enquanto a estratégia é plana, o volume aumenta em `VolumeIncrement` e o limite dobra (4 → 8 → 16, etc.). Se um dos parâmetros for definido como `0`, a regra de escalabilidade será desativada.

## Parâmetros
* **Período EMA rápido** – duração do filtro de tendência rápida (padrão: 33).
* **Período EMA lenta** – duração do filtro de tendência lenta (padrão: 70).
* **Williams Período %R** – lookback para o oscilador Williams %R (padrão: 62).
* **Williams Nível de venda** – limite superior que deve ser cruzado para baixo para sinais vendidos (padrão: −20).
* **Williams Nível de compra** – limite inferior que deve ser cruzado para cima para sinais longos (padrão: −80).
* **CCI Período** – lookback para o Commodity Channel Index (padrão: 26).
* **CCI Nível de venda** – valor mínimo de CCI necessário para permitir vendas (padrão: 101).
* **CCI Nível de compra** – valor máximo de CCI necessário para permitir posições compradas (padrão: 99).
* **Donchian Comprimento** – número de velas concluídas amostradas para a saída do breakout (padrão: 410).
* **Max Open Positions** – máximo de negociações simultâneas; a configuração clássica usa um contrato (padrão: 1).
* **Take Profit (pips)** – meta de lucro medida em etapas de preço (padrão: 360).
* **Stop Loss (pips)** – stop de proteção medido em etapas de preço (padrão: 20).
* **Volume de entrada** – tamanho inicial do pedido (padrão: 0,01).
* **Limiar de Grande Vitória** – lucro realizado necessário antes de aumentar o tamanho (padrão: 4,0).
* **Incremento de volume** – volume adicional adicionado após ultrapassar o limite (padrão: 0,01).
* **Tipo de vela** – período usado para todos os cálculos do indicador (padrão: velas horárias).

## Notas adicionais
* A conversão de pip depende do `PriceStep` do título. Os instrumentos sem variação de preço voltam para um valor unitário de pip.
* Os níveis de Donchian são intencionalmente atrasados por uma vela para que a saída espelhe a lógica `shift=1` das chamadas MetaTrader originais.
* A escala de volume avalia apenas o PnL realizado enquanto a posição é plana, evitando que ganhos flutuantes acionem falsos positivos.
* Os objetos de rótulo da IU presentes na fonte EA são omitidos porque StockSharp visualiza o estado por meio de gráficos e registros.
* Somente a implementação C# é fornecida neste pacote; não existe uma versão Python.
