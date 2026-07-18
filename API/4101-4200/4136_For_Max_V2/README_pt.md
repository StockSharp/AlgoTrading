# Para máximo v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Para Max V2 é uma porta do MetaTrader 4 consultor especialista `for_max_v2.mq4`. A estratégia espera por padrões específicos de consumo de duas velas e, em seguida, coloca um par simétrico de ordens de compra e parada de venda em torno da vela mais recente. Depois que uma ordem de rompimento é preenchida, a ordem pendente oposta é removida e a posição é gerenciada com paradas fixas, níveis de take-profit opcionais e uma rotina de rastreamento que primeiro bloqueia um pequeno lucro no ponto de equilíbrio e depois segue o preço.

## Lógica estratégica
### Detecção de padrões envolventes
O consultor especialista original expõe dois blocos de entrada e ambos são preservados:
* **Configuração tipo 1** – verifica as velas `Max Search` anteriores (pulando a barra atual) e espera que a mínima mais baixa dentro desse intervalo ocorra há duas barras **ou** que a máxima mais alta ocorra há duas barras. Quando isso acontece, a vela duas barras atrás deve engolir a vela anterior (máxima mais alta e mínima mais baixa). A configuração envolve a vela acabada mais recente.
* **Configuração tipo 2** – também verifica as velas `Max Search` anteriores, mas procura o extremo que apareceu uma barra atrás. Além disso, a vela uma barra atrás deve engolir a vela duas barras atrás. Um straddle é então colocado em torno da vela mais recente. Ambas as configurações podem coexistir; cada um gerencia seus próprios pedidos pendentes e relógio de vencimento.

### Colocação de pedido pendente
* **Preços de entrada** – as ordens de compra são colocadas na máxima da vela anterior mais `Gap Points`, as ordens de stop de venda na mínima da vela anterior menos `Gap Points`.
* **Stop-loss** – para o Tipo 1, o stop longo está ancorado na mínima da vela duas barras atrás (menos o gap) e o stop curto na máxima dessa vela (mais o gap). O tipo 2 usa a vela anterior para ambos os lados.
* **Realização de lucro** – opcional. Os alvos longos somam `Gap Points + Buy Take Profit Points` à máxima anterior e os vendidos subtraem `Gap Points + Sell Take Profit Points` da mínima anterior. Definir as entradas de lucro como `0` desativa as respectivas metas.
* **Expiração** – cada straddle carrega um carimbo de data/hora de validade calculado como `Order Expiry (bars)` multiplicado pelo período de vela configurado. Se as ordens pendentes ainda estiverem funcionando quando o carimbo de data/hora for atingido, ambos os lados serão cancelados.

### Gestão de posição
* Assim que um buy-stop for preenchido, quaisquer ordens de sell-stop restantes de qualquer configuração serão canceladas; a regra simétrica se aplica após uma entrada curta.
* Stops e metas são monitorados em velas concluídas. Se a mínima de uma vela atingir o stop longo (ou a máxima atingir o stop curto), a posição será fechada com uma ordem de mercado. A mesma abordagem é usada para os níveis de take-profit.
* A rotina de ponto de equilíbrio (`Break-even Trigger` e `Break-even Offset`) move o stop para o preço de entrada mais/menos o deslocamento configurado assim que a posição avança pelo valor de gatilho.
* O bloco final mantém os pontos de parada `Long/Short Trailing Buffer` longe da melhor excursão, mas somente depois que o preço tiver percorrido uma distância suficiente (e, opcionalmente, somente depois que a negociação já for lucrativa). `Trailing Step` evita ajustes excessivamente frequentes, exigindo uma melhoria mínima antes que o stop seja apertado novamente.

## Parâmetros
* **Volume** – volume da ordem para cada ordem stop pendente.
* **Buy Take Profit (pontos)** – distância em pontos usada para calcular o longo take-profit (definido como `0` para desativar).
* **Sell Take Profit (pontos)** – distância em pontos usada para calcular o short take-profit (definido como `0` para desativar).
* **Gap (pontos)** – buffer adicionado aos máximos/mínimos antes de colocar entradas de stop e dobrado na distância de take-profit.
* **Profundidade de pesquisa** – número de velas concluídas digitalizadas ao verificar configurações de engolfamento Tipo 1 e Tipo 2.
* **Expiração do pedido (barras)** – número de comprimentos de vela que um straddle pendente permanece ativo antes de ambos os lados serem cancelados.
* **Gatilho do ponto de equilíbrio (pontos)** – limite de lucro que ativa o ajuste do ponto de equilíbrio.
* **Compensação do ponto de equilíbrio (pontos)** – buffer adicional adicionado ao preço de entrada quando o stop de equilíbrio é colocado.
* **Long Trailing Buffer (pontos)** – distância final para posições longas quando o ponto de equilíbrio for atingido.
* **Short Trailing Buffer (pontos)** – distância final para posições curtas quando o ponto de equilíbrio for atingido.
* **Trailing Step (pontos)** – melhoria mínima na localização da parada necessária antes de atualizar o trailing stop novamente.
* **Trail Only After Profit** – se habilitado, o trailing espera até que a posição ultrapasse o buffer antes de ser ativado.
* **Tipo de vela** – período de tempo das velas usadas para detecção de padrões, vencimento de pedidos e processamento de saída.

## Notas adicionais
* As compensações de preço expressas em “pontos” dependem do `PriceStep` do título. Símbolos com cinco (ou três) casas decimais são convertidos automaticamente em tamanhos de pip fracionários, assim como em MetaTrader.
* Stop Loss e Take Profits são executados por meio de ordens de mercado dentro da estratégia para espelhar o comportamento do EA de gerenciar níveis em velas fechadas.
* A estratégia não implementa a função `vhod_3` não utilizada da fonte original; apenas os dois blocos de entrada ativos foram portados.
* Este pacote contém apenas a implementação C#; nenhuma versão do Python é fornecida.
