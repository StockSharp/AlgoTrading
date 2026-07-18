# Estratégia de Cesta de Trader de Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista "Divergence Trader" MetaTrader. Ele compara duas médias móveis simples
calculado em fontes de preços configuráveis e mede sua diferença (divergência). Quando a distância entre o rápido e o lento
médias caem dentro de um corredor neutro, o algoritmo assume que o momentum está prestes a ser retomado e abre uma posição no
direção do viés predominante. A implementação usa apenas velas concluídas de um período selecionado e depende do
API de alto nível com ligações de indicadores.

## Parâmetros
- **Tamanho do Lote** – volume de negociação enviado com cada nova posição. O valor está alinhado com o passo de volume do instrumento.
- **Período/preço SMA rápido** – duração e fonte de preço para a média móvel rápida.
- **Período/preço lento SMA** – duração e fonte de preço para a média móvel lenta.
- **Limiar de Compra** – divergência positiva mínima necessária antes de abrir uma posição longa.
- **Stay-Out Threshold** – divergência máxima permitida para novas entradas; valores fora desta faixa desabilitam a negociação.
- **Take Profit (pips)** – meta de lucro expressa em pips. Desativado quando definido como zero.
- **Stop Loss (pips)** – tolerância a perdas em pips. Desativado quando definido como zero.
- **Trailing Stop (pips)** – distância móvel ativada após a negociação se tornar lucrativa. Desativado quando zero.
- **Break-Even Trigger / Buffer (pips)** – ganho de pip necessário antes de proteger a posição no ponto de equilíbrio e buffer opcional para
compensar o ponto de equilíbrio do preço de entrada.
- **Basket Lucro/Basket Loss** – limites baseados no patrimônio da conta que nivelam todas as posições quando alcançados. O controle de perdas é
desativado por padrão.
- **Start Hour / Stop Hour** – janela de negociação no horário local. Quando ambos os valores são iguais, a estratégia funciona o dia todo.
- **Tipo de vela** – período de tempo usado tanto para geração de sinal quanto para gerenciamento de risco.

## Lógica de negociação
1. Assine a série de velas configurada e calcule as médias móveis simples rápidas e lentas.
2. Trabalhe apenas com velas prontas para evitar ruído intrabarra e ficar próximo do comportamento original EA.
3. Acompanhe a divergência (rápida menos lenta) calculada na vela finalizada anteriormente:
   - Se a divergência for positiva e permanecer entre o **Limiar de Compra** e o **Limiar de Permanência**, envie uma ordem de compra a mercado.
   - Se a divergência for negativa e seu valor absoluto permanecer dentro do corredor, envie uma ordem de venda a mercado.
4. As negociações são ignoradas fora do horário permitido ou quando a estratégia já possui uma posição aberta.

## Gerenciamento de posição
- **Controle de ponto de equilíbrio** – quando o lucro flutuante atinge o gatilho, a estratégia armazena um nível de stop de ponto de equilíbrio (opcionalmente
deslocado pelo buffer). Uma vela que atinge este nível fecha a posição.
- **Trailing stop** – quando o lucro excede a distância final, o nível de stop segue o preço mais favorável, embora sempre
ficando atrás dele pelo número configurado de pips.
- **Take Profit/Stop Loss** – saídas fixas calculadas a partir do preço de entrada em unidades pip.
- **Proteção de cesta** – o patrimônio do portfólio é comparado com os limites de lucros e perdas configurados. Atingindo qualquer limite
fecha a posição atual e cancela as ordens ativas, emulando a rotina "CloseEverything" da versão MQL.

## Notas de uso
- O corredor de divergência é simétrico: ampliar o **Limite de permanência** permite que as negociações permaneçam abertas por mais tempo, ao mesmo tempo que o estreita
aumenta a frequência dos sinais.
- As opções de origem de preço correspondem a valores StockSharp `CandlePrice`, possibilitando o uso de abertura, fechamento, mediana ou típica
preços como em MetaTrader.
- A estratégia traça velas, tanto médias móveis quanto ordens preenchidas, em uma área do gráfico para monitoramento e depuração.
- Os recursos de gerenciamento de dinheiro dependem dos dados do portfólio. Ao executar em um sandbox sem estatísticas de portfólio, os controles de cesta são
ignorado automaticamente.
