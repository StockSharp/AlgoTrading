# Estratégia de gatilho do Macd Pattern Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de gatilho do Macd Pattern Trader transporta o MetaTrader 4 consultor especialista `MacdPatternTraderv05cb` para a estratégia de alto nível de StockSharp API. O sistema negocia padrões de histograma MACD puros, procurando uma estrutura de topo duplo abaixo da linha zero para abrir posições vendidas e uma imagem espelhada de fundo duplo acima da linha zero para abrir posições compradas. A gestão comercial reflete o EA original: cada entrada é enviada no mercado com um stop loss fixo configurável e um take-profit medido em pontos de instrumento.

## Lógica estratégica
### Fluxo de indicadores
* Uma única assinatura de vela conduz a lógica (padrão: velas de 15 minutos). Cada vela finalizada alimenta um indicador `MovingAverageConvergenceDivergence` configurado com os parâmetros incomuns do MT4 `(fast = 13, slow = 5, signal = 1)` usados ​​pela fonte EA.
* Apenas a linha principal MACD é usada. A estratégia armazena em buffer os três últimos valores concluídos para emular `iMACD(..., MODE_MAIN, shift=1..3)` de MetaTrader.

### Configuração de alta (entradas longas)
1. **Condição de armar** – a linha MACD deve subir acima de `Bullish Trigger` (padrão `0.0015`). Isso prepara a estratégia para procurar a sequência de pullback. Qualquer queda abaixo de zero limpa o estado imediatamente.
2. **Janela de pullback** – uma vez armado, o MACD deve voltar abaixo de `Bullish Reset` (padrão `0.0005`). Isso marca a área potencial de retração. A janela permanece ativa até que um padrão válido seja confirmado ou MACD se torne negativo.
3. **Confirmação de padrão** – enquanto a janela estiver ativa, as últimas três leituras MACD armazenadas em buffer devem satisfazer:
   * `macd_curr > macd_last` (o impulso volta a aumentar),
   * `macd_last < macd_last3` (a barra anterior definiu o swing baixo),
   * `macd_curr > Bullish Reset` e `macd_last < Bullish Reset` (recuperação de preço da zona de retração superficial).
4. **Execução** – quando confirmada, a estratégia compra a mercado. Se existir uma posição curta, o tamanho da ordem inclui automaticamente o volume necessário para estabilizar antes de estabelecer a exposição longa.

### Configuração de baixa (entradas curtas)
1. **Condição de armar** – a linha MACD deve ficar abaixo de `-Bearish Trigger` (padrão `-0.0015`). Qualquer movimento acima de zero limpa todo o estado de baixa.
2. **Janela de pullback** – uma vez armado, o MACD deve saltar acima de `-Bearish Reset` (padrão `-0.0005`).
3. **Confirmação do padrão** – enquanto a janela estiver aberta, os valores armazenados em buffer devem satisfazer:
   * `macd_curr < macd_last`,
   * `macd_last > macd_last3`,
   * `macd_curr < -Bearish Reset` e `macd_last > -Bearish Reset`.
4. **Execução** – uma ordem de venda a mercado é enviada. Se existir uma posição comprada, seu volume será incluído na ordem, de modo que a conta fique líquida vendida pelo tamanho de negociação configurado.

### Gestão de risco
* **Stop Loss/Take Profit fixos** – as distâncias são especificadas em pontos (etapas de preço). A estratégia os multiplica pelo `PriceStep` do instrumento e chama `StartProtection` para reproduzir o comportamento original do SL/TP. Definir uma distância para `0` desativa o respectivo nível.
* **Um sinal por janela** – após fazer um pedido, os sinalizadores de armar e de janela são apagados para evitar entradas repetidas do mesmo padrão MACD.

## Parâmetros
* **Volume de negociação** – volume de ordens de mercado. As posições opostas são fechadas automaticamente antes de abrir a nova negociação.
* **EMA rápida / EMA lenta / Sinal EMA** – MACD comprimentos. Os padrões replicam o orientador original, mas podem ser otimizados.
* **Gatilho / Redefinição de alta** – limites MACD positivos (em unidades de indicador) que armam a configuração longa e definem sua zona de pullback.
* **Gatilho/redefinição de baixa** – limites absolutos de MACD para a configuração curta. O gatilho é aplicado com um sinal negativo durante o tempo de execução.
* **Stop Loss / Take Profit** – distâncias em pontos (etapas de preço). Um valor de `0` desativa a proteção correspondente.
* **Tipo de vela** – período usado para cálculo de MACD e decisões de negociação.

## Notas de implementação
* O StockSharp API de alto nível é usado em: `SubscribeCandles` alimenta o indicador e `StartProtection` espelha o gerenciamento de negociação MT4.
* O buffer de histórico MACD garante que a lógica de decisão opere nas três barras concluídas anteriores, correspondendo às chamadas `shift=1..3` de MetaTrader.
* Não há versão Python desta estratégia no pacote API, apenas a implementação C#.
