# Estratégia EA Trix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A estratégia EA Trix replica a lógica do assessor especialista MetaTrader 5 que combina o indicador *TRIX ARROWS* com
ferramentas básicas de gestão de risco. O sistema aguarda que a média móvel exponencial tripla (TRIX) e sua linha de sinal
se cruzem antes de entrar em novas posições. Pode reagir imediatamente na vela de sinal ou atrasar a execução até a próxima
barra, emulando o comportamento original de "operar no fechamento da barra".

## Lógica de Trading

1. Construir duas médias móveis exponenciais triplicadas:
   - TRIX é calculado aplicando três EMAs com o comprimento **TRIX EMA** ao fechamento da vela e tomando a taxa de mudança
     de uma barra do terceiro suavizamento.
   - A linha de sinal é calculada da mesma forma, mas usa o comprimento **Signal EMA**.
2. Detectar mudanças de direção através de cruzamentos:
   - Quando a linha de sinal cruza **acima** do TRIX, a estratégia prepara uma entrada comprada.
   - Quando a linha de sinal cruza **abaixo** do TRIX, ela prepara uma entrada vendida.
3. Dependendo da configuração **Trade On Close**, a estratégia:
   - Executa imediatamente ao preço de fechamento da barra de sinal; ou
   - Coloca a ordem na fila e a executa na abertura da próxima barra (correspondendo à opção do EA MT5 para operar em barras
     fechadas).
4. Antes de abrir uma nova posição, o algoritmo reverte automaticamente qualquer exposição contrária para que apenas uma
   posição líquida exista a qualquer momento.

## Gestão de Posições

- **Stop loss** – distância fixa opcional a partir do preço de preenchimento. Desabilitado quando definido como zero.
- **Take profit** – alvo de lucro opcional. Desabilitado quando definido como zero.
- **Break-even** – uma vez que o preço avança a favor da operação pela distância selecionada, o stop é movido para o preço
  de entrada.
- **Trailing stop** – após o preço se mover pela distância de trailing, o stop segue o preço com o incremento mínimo de
  **Trailing Step** selecionado.
- As saídas protetoras são avaliadas em cada vela completada usando os valores high/low da vela. Quando uma saída protetora
  é acionada, a posição é fechada com uma ordem de mercado.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `CandleType` | Tipo de dados (período) das velas processadas pela estratégia. |
| `Volume` | Tamanho de posição usado para novas entradas. As posições existentes são revertidas automaticamente quando necessário. |
| `EmaPeriod` | Comprimento das médias móvies exponenciais usadas para calcular a curva TRIX. |
| `SignalPeriod` | Comprimento das médias móveis exponenciais usadas para calcular a curva de sinal. |
| `TradeOnCloseBar` | Se `true`, as entradas são colocadas na fila e executadas na abertura da próxima barra. Se `false`, a execução acontece imediatamente no fechamento da barra de sinal. |
| `StopLoss` | Distância do preço de entrada ao stop protetor. Definir como `0` para desabilitar. |
| `TakeProfit` | Distância ao alvo de lucro. Definir como `0` para desabilitar. |
| `TrailingStop` | Distância para o trailing stop ser ativado. Definir como `0` para desabilitar. |
| `TrailingStep` | Incremento mínimo aplicado ao atualizar o trailing stop. |
| `BreakEven` | Distância necessária para mover o stop ao preço de entrada. Definir como `0` para desabilitar. |

## Notas de Uso

- A estratégia subscreve um único feed de velas e depende exclusivamente de velas completadas conforme exigido pelas
  diretrizes da API de alto nível do StockSharp.
- As distâncias padrão de gestão de risco são expressas em unidades de preço. Ajuste-as de acordo com o tamanho do tick
  do instrumento negociado.
- Como as ordens são enviadas via comandos de mercado, o preço de preenchimento é assumido como o fechamento da vela (ou
  abertura para sinais na fila) em backtests.

## Notas de Conversão

- O expert MQL5 original usa o indicador externo *TRIX ARROWS* (código 19056). A conversão reconstrói os mesmos cálculos
  usando instâncias de `ExponentialMovingAverage` do StockSharp e lógica de taxa de mudança sem depender de buffers
  personalizados.
- O gerenciamento de risco do MT5 dependia de ordens stop e limit do lado do broker. No StockSharp, as saídas protetoras são
  replicadas monitorando os extremos das velas e emitindo ordens de mercado.
- Alertas, notificações de som e parâmetros específicos do broker foram omitidos porque não fazem parte da lógica de trading
  central.
