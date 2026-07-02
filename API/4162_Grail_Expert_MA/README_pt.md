# Especialista em Graal MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Grail Expert MA é uma versão StockSharp do MetaTrader 4 consultor especialista `_GrailExpertMAV1_0`. O sistema procura por novos rompimentos além do canal máximo/mínimo recente e aguarda um retrocesso antes de aderir ao movimento. Uma média móvel exponencial do preço típico fornece o viés direcional: as negociações só são permitidas quando o EMA ganhou ou perdeu um número configurável de pips nas duas últimas velas concluídas. A gestão de risco reflete o especialista original com distâncias de stop-loss e take-profit baseadas em pip e ignora novas entradas enquanto uma posição está ativa.

## Lógica estratégica
### EMA filtro de tendência de inclinação
* Um EMA calculado sobre o preço típico ((High + Low + Close)/3) é avaliado no fechamento de cada barra.
* A diferença entre os dois últimos valores EMA deve exceder o limite `EMA Slope (pips)` (convertido em preço usando o símbolo pip size).
* Uma inclinação positiva autoriza retrocessos longos, uma inclinação negativa autoriza retrocessos curtos e inclinações planas bloqueiam a negociação.

### Rastreamento de intervalo de breakout
* A estratégia mantém a máxima mais alta e a mínima mais baixa nas últimas `Range Period` barras concluídas.
* Esses níveis formam um canal cuja altura é utilizada para rejeitar movimentos rasos que não criam distância suficiente para a lógica de pullback.

### Preparação de entrada
* Quando a barra atual imprime uma nova máxima acima do intervalo armazenado, um potencial preço de entrada longo é calculado em `High - Breakout Buffer - Take Profit` pips.
* Quando a barra atual imprime um novo mínimo abaixo da faixa armazenada, um potencial preço de entrada vendido é calculado em `Low + Breakout Buffer + Take Profit` pips.
* O EA original exigia que a distância entre o novo extremo e o lado oposto do intervalo fosse de pelo menos `2 * Breakout Buffer + Take Profit`. A porta mantém a mesma validação e descarta a entrada se o spread for muito pequeno.

### Gatilho de entrada
* Os preços preparados permanecem ativos durante o restante da barra. Uma compra é executada quando o mínimo intrabarra atinge ou cai abaixo do preço de entrada comprado armazenado enquanto a inclinação EMA é positiva.
* Uma venda é executada quando a máxima intrabarra atinge ou excede o preço de entrada vendido armazenado enquanto a inclinação EMA é negativa.
* Apenas uma negociação pode ser aberta por vez; a porta libera ambos os preços de entrada pendentes assim que um pedido é enviado para corresponder ao comportamento MQL.

### Gerenciamento de saída
* As posições longas usam um stop em `Entry - Stop Loss` pips e uma meta de lucro em `Entry + Take Profit` pips (zero desativa o respectivo nível).
* As posições curtas refletem os cálculos (stop acima, objetivo abaixo).
* As saídas são acionadas quando os extremos da vela tocam os níveis de proteção, correspondendo à aproximação baseada em barras da lógica de tick original.

### Salvaguardas adicionais
* As entradas pendentes são apagadas sempre que ficam fora do intervalo atualizado quando uma nova vela fecha.
* Todas as distâncias de pip se adaptam automaticamente ao tamanho do tick do instrumento (símbolos FX de cinco dígitos mapeiam um pip para 10 ticks).
* Se o EMA ainda não estiver formado ou o buffer de intervalo não tiver histórico suficiente, a estratégia permanecerá inativa até que dados suficientes estejam disponíveis.

## Parâmetros
* **Volume de Ordens** – volume de negociação em lotes/contratos para ordens de mercado.
* **Take Profit (pips)** – distância até a meta de lucro fixa; defina como `0` para desativar.
* **Stop Loss (pips)** – distância até o stop de proteção; defina como `0` para desativar.
* **Período de intervalo** – número de velas concluídas usadas para medir o canal de breakout.
* **EMA Período** – duração da média móvel exponencial aplicada ao preço típico.
* **EMA Inclinação (pips)** – avanço/declínio mínimo de pip entre valores EMA consecutivos necessários para ativar entradas.
* **Breakout Buffer (pips)** – distância adicional do novo extremo antes de armar entradas de pullback.
* **Tipo de vela** – período solicitado no feed de dados (padrão: velas de 1 hora).

## Notas de implementação
* A estratégia usa atualizações brutas de velas (incluindo estados parciais) para emular o monitoramento intrabar original de alta/baixa.
* Os valores EMA são processados apenas em velas finalizadas para replicar as chamadas MQL `iMA` com mudanças de uma e duas barras.
* Os intervalos históricos são rastreados com filas limitadas em vez de pesquisas de indicadores para evitar novas varreduras dispendiosas e, ao mesmo tempo, manter a lógica fiel à fonte.
* Nenhuma versão do Python é fornecida; o pacote API contém apenas a implementação C#.
