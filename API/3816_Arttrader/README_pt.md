# Estratégia Arttrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Arttrader é uma conversão do MetaTrader 4 consultor especialista `Arttrader_v1_5`. O sistema opera com velas horárias e tenta capturar movimentos direcionais suaves medidos por uma média móvel exponencial (EMA) do preço de abertura. As entradas são filtradas pela inclinação EMA e por uma verificação rigorosa da posição do preço intrabar, enquanto uma proteção de volatilidade dedicada bloqueia negociações após grandes lacunas de abertura. As posições são gerenciadas por meio de um procedimento de stop-loss cronometrado, níveis fixos de parada de emergência e take-profit e um sistema de segurança contra falhas baseado em volume.

A porta StockSharp mantém as entradas originais e executa negociações por meio de ordens de mercado de alto nível. Todos os cálculos são realizados em velas prontas; os requisitos de tempo intrabar do consultor especialista são aproximados comparando os atrasos de minutos configurados com a duração da vela.

## Lógica estratégica
### Indicador
* **Preço de abertura EMA** – um único EMA com período configurável (`EMA Speed`) é calculado sobre o preço de abertura da vela. A diferença entre os valores EMA atuais e anteriores define a inclinação em pips.

### Filtros
* **Limites de inclinação** – a inclinação EMA deve estar entre os limites mínimo (`Slope Min`) e máximo (`Slope Max`). A estratégia ignora as negociações quando a tendência é muito fraca ou muito forte.
* **Alinhamento intrabar** – negociações longas exigem que a vela feche abaixo ou igual à sua abertura e permaneça dentro do mínimo mais o deslizamento de entrada configurado. As negociações curtas refletem a condição em torno da alta. Os parâmetros de atraso (`Entry Delay`, `Exit Delay`) são satisfeitos quando a duração da vela atual é pelo menos igual aos minutos configurados.
* **Proteção contra pico de volatilidade** – avalia as diferenças de abertura para abertura nas últimas cinco velas. Se qualquer intervalo único exceder `Big Jump` pips, ou qualquer intervalo de duas barras exceder `Double Jump` pips, novas entradas serão bloqueadas para a barra atual.

### Entradas
* **Entrada longa** – acionada quando todos os filtros passam, a inclinação EMA é positiva e não há posição existente. O preço de entrada sintético armazenado é ajustado pelo parâmetro `Spread Adjust` para emular a compensação do spread original.
* **Entrada curta** – lógica simétrica que requer uma inclinação EMA negativa e nenhuma posição ativa.

### Saídas
* **Parada inteligente cronometrada** – uma vez no lucro ou prejuízo, a estratégia avalia a parada inteligente somente depois que o requisito `Exit Delay` for satisfeito. Para posições compradas, exige que o fechamento esteja acima da abertura e suficientemente próximo da máxima, enquanto a perda em pips em relação ao preço de entrada sintético deve exceder `Smart Stop`.
* **Volume à prova de falhas** – se o volume da vela concluída anteriormente for menor ou igual a `Min Volume`, qualquer posição aberta será fechada imediatamente na próxima barra.
* **Parada de emergência/take-profit** – assim que uma negociação é aberta, uma parada de emergência forte e um nível fixo de take-profit são registrados. Se o intervalo da vela atingir qualquer um dos níveis, a posição será fechada sem esperar pelos filtros temporizados.

## Parâmetros
* **Volume de Pedidos** – tamanho da negociação usado para ordens de mercado.
* **EMA Período** – duração do EMA aplicado às aberturas da vela.
* **Big Jump (pips)** – maior intervalo permitido de abertura de barra única antes que os sinais de entrada sejam suprimidos.
* **Salto duplo (pips)** – maior intervalo de abertura permitido de duas barras antes que os sinais de entrada sejam suprimidos.
* **Smart Stop (pips)** – distância pip necessária para acionar a lógica de stop-loss cronometrada.
* **Parada de Emergência (pips)** – distância de parada brusca avaliada em cada vela alta/baixa.
* **Take Profit (pips)** – distância fixa de take-profit avaliada em cada vela alta/baixa.
* **Slope Min / Slope Max (pips)** – EMA limites de inclinação para elegibilidade comercial.
* **Atraso de entrada (min)** – duração mínima da vela (em minutos) antes que as entradas sejam permitidas.
* **Atraso de saída (min)** – duração mínima da vela (em minutos) antes que a parada cronometrada possa ser executada.
* **Entry Slip / Exit Slip (pips)** – tolerância entre o fechamento e o extremo na validação de filtros de entrada e saída.
* **Volume Mínimo** – volume mínimo da vela anterior; as negociações são fechadas se o valor não for ultrapassado.
* **Spread Adjust (pips)** – compensação de spread sintético aplicada ao preço de entrada armazenado.
* **Slippage (pips)** – configuração informativa preservada para compatibilidade com as entradas MetaTrader.
* **Tipo de vela** – período usado para assinaturas de velas (o padrão é velas de 1 hora).

## Notas
* A implementação StockSharp executa ordens de mercado e compensa posições usando `BuyMarket`/`SellMarket`, correspondendo ao comportamento de posição única do EA original.
* Como StockSharp opera em velas concluídas, as verificações de minutos intrabarras de MetaTrader são aproximadas comparando os atrasos configurados com a duração total da vela.
* Os níveis de parada de emergência e take-profit são avaliados em relação aos máximos e mínimos das velas, emulando as ordens de proteção do corretor da versão MetaTrader.
