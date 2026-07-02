# MACD Estratégia de Divergência RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porta do consultor especialista MetaTrader **"Macd diver rsi mt4"** para o StockSharp API de alto nível.
- Negocia um único símbolo usando filtros RSI combinados com reconhecimento de divergência MACD para reversões de tempo.
- Apenas uma posição de mercado pode ser aberta por vez; a estratégia espera pelo estado plano antes de emitir um novo sinal.

## Lógica de Sinais
1. Cada vela finalizada no período selecionado alimenta quatro indicadores vinculados à estratégia:
   - Duas instâncias independentes de `RelativeStrengthIndex` (para filtros de sobrevenda e sobrecompra) amostraram uma barra atrás.
   - Dois indicadores `MovingAverageConvergenceDivergence` com EMA rápido/lento configurável e comprimentos de sinal.
2. **Configuração de alta**
   - A barra anterior RSI deve estar abaixo do limite de sobrevenda configurável.
   - Os valores MACD mais recentes devem formar uma queda local abaixo de um limite dinâmico (equivalente a 3 pips no instrumento atual).
   - Os dados históricos são verificados para localizar uma queda anterior de MACD e a oscilação de preço associada. A divergência é confirmada quando
o vale MACD sobe enquanto o preço atinge um mínimo mais baixo (divergência regular) ou o vale MACD cai enquanto o preço sobe
baixo (divergência oculta), correspondendo à lógica MQL original.
   - Quando confirmada e a estratégia não tem posição aberta, uma compra de mercado é enviada com volume específico e configurações de risco.
3. **Configuração de baixa** reflete as regras de alta com o filtro de sobrecompra RSI e picos de MACD. A divergência é validada por
comparando os máximos de swing anteriores com os atuais.
4. Imediatamente após uma entrada, a estratégia converte as distâncias configuradas de stop-loss e take-profit de pips em unidades de preço
(respeitando as regras originais de formato de ponto) e as aplica através de `SetStopLoss` / `SetTakeProfit`.

## Parâmetros
- `LowerRsiPeriod`, `LowerRsiThreshold` – mapeia para `inp1_Lo_RSIperiod` / `inp1_Ro_Value`.
- `BullishFastEma`, `BullishSlowEma`, `BullishSignalSma` – mapear para `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA`.
- `BullishVolume`, `BullishStopLossPips`, `BullishTakeProfitPips` – mapear para `inp3_VolumeSize`, `inp3_StopLossPips`, `inp3_TakeProfitPips`.
- `UpperRsiPeriod`, `UpperRsiThreshold` – mapeia para `inp4_Lo_RSIperiod` / `inp4_Ro_Value`.
- `BearishFastEma`, `BearishSlowEma`, `BearishSignalSma` – mapear para `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA`.
- `BearishVolume`, `BearishStopLossPips`, `BearishTakeProfitPips` – mapear para `inp6_VolumeSize`, `inp6_StopLossPips`, `inp6_TakeProfitPips`.
- `CandleType` – fonte do período para todos os cálculos.

## Notas de implementação
- O limite de divergência MACD é derivado do tamanho atual do ponto do instrumento e é igual a 3 pips, correspondendo ao padrão 0,0003
usado pela versão MQL.
- Candle, MACD e histórico de preços são armazenados em listas limitadas (600 elementos) para reproduzir as janelas de varredura de divergência sem
alocando grandes matrizes.
- A estratégia usa `SubscribeCandles(...).Bind(...)` para atualizar todos os indicadores em uma única passagem e os processos só são finalizados
velas, assim como a execução original do bloco uma vez por barra.
- As distâncias pip são convertidas em compensações de preço absoluto antes de chamar `SetStopLoss` e `SetTakeProfit`, reproduzindo o
regras de formato de ponto declaradas no topo da fonte MQL.
