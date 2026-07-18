# Estratégia N7S AO 772012
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma conversão StockSharp do consultor especialista MetaTrader **N7S_AO_772012**. O robô original combina filtros do tipo perceptron do Awesome Oscillator (AO) em vários intervalos de tempo com uma porta de padrão de preço e um modo "neuro" configurável que pode substituir a lógica básica. A versão convertida preserva a árvore de decisão enquanto expõe todos os botões de ajuste como parâmetros de estratégia.

O bot opera no instrumento principal selecionado na estratégia e utiliza:

- **Velas M1** para tempo de entrada e percepção de preço.
- **Velas H1** para alimentar vários perceptrons baseados em AO.
- **Velas H4** para calcular o delta do momentum AO usado pelo seletor base de compra/venda (BTS).

## Lógica de negociação

1. Em cada vela M1 finalizada, a estratégia atualiza o histórico do padrão de preços, gerencia as posições existentes e avalia se a negociação é permitida (nenhuma negociação na segunda-feira antes das 02h00 ou na sexta-feira a partir das 18h00, horário local da plataforma).
2. Os valores horários de AO são agregados em cinco perceptrons:
   - `Perceptron X/Y` – filtros BTS básicos que funcionam em conjunto com o perceptron de preço e o delta H4 AO.
   - `Neuro X/Y` – filtros longos/curtos avançados usados quando o modo neuro concede prioridade a eles.
   - `Neuro Z` – perceptron de ativação que ativa o Neuro X no modo 4.
3. O perceptron de preço avalia as diferenças ponderadas entre as aberturas recentes do M1 e o fechamento mais recente.
4. The **neuro mode** parameter controls how the uppercase perceptrons intervene:
   - `4`: Se Neuro Z > 0, apenas Neuro X pode gerar um sinal longo; caso contrário, o Neuro Y pode desencadear um curto. Se nenhum deles disparar, volte para o BTS.
   - `3`: Neuro Y pode desencadear shorts; caso contrário, volte para o BTS.
   - `2`: Neuro X pode desencadear posições compradas; caso contrário, volte para o BTS.
   - Qualquer outro valor ignora a camada neuro e avalia diretamente o BTS.
5. O bloco **BTS** usa o perceptron de preço e o delta H4 AO como portões:
   - Configuração longa: perceptron de preço > 0 (a menos que `BtsMode = 0`), Neuro/BTS X > 0 e H4 AO delta > 0. Stop-loss é `BaseStopLossPointsLong`, take-profit é `BaseTakeProfitFactorLong × BaseStopLossPointsLong`.
   - Configuração curta: perceptron de preço < 0 (a menos que `BtsMode = 0`), Neuro/BTS Y > 0 e H4 AO delta < 0. Stop-loss é `BaseStopLossPointsShort`, take-profit é `BaseTakeProfitFactorShort × BaseStopLossPointsShort`.
6. Após a aceitação de um sinal, a estratégia abre uma ordem de mercado (respeitando a direção habilitada). Os preços de proteção são rastreados internamente; cada vela M1 finalizada verifica se o stop ou alvo foi atingido usando máximos/mínimos da vela e fecha a posição quando apropriado. Os sinais opostos primeiro fecham a posição existente e aguardam a próxima vela antes da reentrada.

## Parâmetros

### Negociação
- **OrderVolume** – Volume base para todas as ordens de mercado.
- **AllowLongTrades / AllowShortTrades** – Habilite ou desabilite entradas longas ou curtas.
- **BtsMode** – Quando definido como `0` o portão perceptron de preço no BTS é ignorado; caso contrário, seu sinal deve estar alinhado com o comércio.
- **NeuroMode** – Selects how the advanced perceptrons participate (see logic section).

### Perceptrons BTS básicos
- **BaseStopLossPointsLong / BaseTakeProfitFactorLong** – Distância de parada (pontos) e multiplicador para lucro longo.
- **BaseStopLossPointsShort / BaseTakeProfitFactorShort** – Configurações análogas para negociações curtas.
- **PerceptronPeriodX / Y** – deslocamento AO (em barras H1) utilizado pelo respectivo perceptron.
- **PerceptronWeightX1..4 / Y1..4** – Pesos (0–100) das entradas do perceptron; internamente eles são centralizados subtraindo 50.
- **PerceptronThresholdX / Y** – Saída mínima absoluta do perceptron necessária antes de ser considerado válido.

### Filtro de preço
- **PricePatternPeriod** – Número de velas M1 formando cada defasagem no perceptron de preço.
- **PriceWeight1..4** – Pesos (centrados em torno de 50) aplicados às diferenças de preços dentro do perceptron.

### Neuro perceptrons
- **NeuroStopLossPointsLong / NeuroTakeProfitFactorLong** – Multiplicador de Stop e TP usado pelos sinais Neuro X.
- **NeuroStopLossPointsShort / NeuroTakeProfitFactorShort** – Stop and TP multiplier used by Neuro Y signals.
- **NeuroPeriodX / Y / Z** – Mudança AO (velas H1) para os três neuro perceptrons.
- **NeuroWeightX1..4 / NeuroWeightY1..4 / NeuroWeightZ1..4** – Pesos Perceptron.
- **NeuroThresholdX / NeuroThresholdY / NeuroThresholdZ** – Valor absoluto mínimo para cada neuro perceptron.

### Dados
- **CandleType** – Período usado para as velas de negociação primárias (padrão 1 minuto).

## Gestão comercial

- As distâncias de stop-loss e take-profit são convertidas de pontos em preços absolutos usando a etapa de preço do instrumento. Se uma distância for definida como zero, a proteção correspondente será desabilitada.
- Os níveis de proteção são monitorados em velas M1 concluídas, comparando os máximos/mínimos das velas com os preços armazenados.
- A estratégia funciona em modo de compensação: nunca mantém posições longas e curtas simultaneamente. Um sinal oposto fecha primeiro a posição atual.

## Notas sobre a conversão

- Ligações StockSharp de alto nível (`SubscribeCandles().Bind(...)`) são usadas para transmitir valores AO sem consultas diretas de indicadores.
- Os buffers históricos são mantidos como listas de tamanho fixo para emular a indexação original baseada em turnos, evitando pesquisas diretas de indicadores.
- Nenhuma versão do Python é fornecida, conforme solicitado.
- Os testes não foram modificados.
