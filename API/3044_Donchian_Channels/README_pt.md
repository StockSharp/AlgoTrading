# Estratégia de Canais Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o clássico expert advisor "Donchian Channels" para a API de alto nível do StockSharp. Combina um rompimento Donchian de múltiplos períodos com médias móveis ponderadas, confirmação de momentum, filtragem de tendência com MACD e extensivos controles de risco (stop loss, take profit, break-even, trailing stop e saída de emergência baseada em equity).

## Visão geral da lógica

- **Regime de mercado:**
  - O Canal Donchian é calculado em um período superior (padrão: 4 horas) para detectar a estrutura de rompimento prevalente.
  - Um MACD calculado em um período de tendência configurável (diário por padrão) garante que a tendência do período superior coincida com a direção da operação.
- **Condições de entrada:**
  - **Configuração comprada:**
    - A banda inferior de Donchian ou a mediana do canal penetra o corpo do candle anterior do período superior por baixo, sinalizando um potencial rompimento.
    - Os dois últimos candles do período base formam um swing ascendente (`Low[2] < High[1]`).
    - O desvio absoluto do momentum a partir de 100 no período superior excede o limiar de compra em qualquer uma das últimas três leituras.
    - A LWMA rápida permanece dentro da distância configurada acima da LWMA lenta para evitar movimentos sobreestendidos.
    - A linha principal do MACD está acima do seu sinal (ambos positivos ou ambos negativos) confirmando viés altista.
  - **Configuração vendida:** Regras simétricas espelhadas para a banda superior de Donchian, estrutura de swing, desvio de momentum baixista e confirmação de MACD.
  - Múltiplas entradas (pirâmide) são permitidas até o número máximo de operações configurado ser alcançado.
- **Condições de saída:**
  - Stop loss e take profit fixos definidos em passos de preço.
  - Movimento opcional para break-even uma vez que o preço progride uma distância configurável além da entrada.
  - Trailing stop que pode seguir os extremos de candles recentes (com relleno) ou trailar o preço usando uma abordagem clássica de gatilho/passo.
  - O stop de equity monitora o drawdown de P&L da estratégia e força o fechamento quando as perdas superam o orçamento de risco permitido.

## Parâmetros

| Grupo | Nome | Descrição |
| ----- | ---- | --------- |
| General | Base Candle | Período de execução para entradas e verificações de risco. |
| General | Donchian Candle | Período superior para o canal Donchian e filtro de momentum. |
| General | Trend Candle | Período usado pelo filtro de tendência MACD. |
| General | Volume | Tamanho da ordem para cada entrada. |
| Indicators | Donchian Length | Período de lookback para o Canal Donchian. |
| Indicators | Fast MA / Slow MA | Comprimentos das médias móveis ponderadas no período de trading. |
| Indicators | MA Distance | Distância máxima permitida entre a LWMA rápida e lenta (em passos de preço). |
| Indicators | Momentum Period | Lookback para o filtro de momentum no período superior. |
| Filtros | Momentum Buy / Sell | Desvio absoluto mínimo a partir de 100 necessário para momentum altista/baixista. |
| Risk | Stop Loss / Take Profit | Saídas fixas medidas em passos de preço a partir do preço de entrada. |
| Risk | Use Trailing | Habilita o gerenciamento do trailing stop. |
| Risk | Trailing Trigger / Step | Parâmetros clássicos de trailing quando o trailing baseado em candles está desativado. |
| Risk | Candle Trail / Trail Candles | Alterna o trailing baseado em candles e define o número de candles utilizados. |
| Risk | Trailing Padding | Buffer extra aplicado em torno dos extremos de candles. |
| Risk | Use BreakEven | Habilita o movimento para break-even. |
| Risk | BreakEven Trigger / Offset | Distância e offset aplicados ao mover o stop para break-even. |
| Risk | Use Equity Stop | Ativa a saída de emergência baseada em drawdown. |
| Risk | Equity Risk | Drawdown máximo permitido antes de fechar a posição. |
| Risk | Max Trades | Número máximo de entradas em pirâmide concorrentes. |

## Dicas de uso

1. **Períodos:** Alinhar o período base com seu estilo de execução (p.ex., 1h/4h) e manter os períodos de Donchian/MACD mais altos para manter a lógica de confirmação multi-período.
2. **Limiares de momentum:** O EA original media desvios de momentum em torno de 100. Começar com limiares pequenos (0.3) e aumentar para filtrar movimentos fracos em mercados agitados.
3. **Configuração de risco:** Converter distâncias baseadas em pips da versão MQL para passos de preço específicos do instrumento. Sempre verificar o valor `Step` do instrumento ao configurar stops e lógica de trailing.
4. **Pirâmide:** Reduzir `Max Trades` para 1 se preferir o gerenciamento de posição única. Aumentar gradualmente ao testar o comportamento de pirâmide.
5. **Stop de equity:** O stop de equity monitora o P&L da estratégia dentro do StockSharp. Ajustar `Equity Risk` para refletir o drawdown máximo (em moeda da conta) que está disposto a tolerar.

## Backtesting

- Funciona diretamente dentro do StockSharp Designer/Backtester usando apenas assinaturas de candles (não são necessários dados de nível tick).
- Garantir que todos os períodos selecionados estejam disponíveis do provedor de dados antes de lançar um backtest ou sessão ao vivo.
- Ao otimizar, priorizar o comprimento de Donchian, a distância de MA e os limiares de momentum — eles têm o impacto mais forte na taxa de acertos e na frequência de operações.
